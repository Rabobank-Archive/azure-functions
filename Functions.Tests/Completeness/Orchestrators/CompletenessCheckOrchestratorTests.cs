using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Functions.Completeness.Orchestrators;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Completeness.Orchestrators
{
    public class CompletenessCheckOrchestratorTests
    {
        private readonly Fixture _fixture;
        public CompletenessCheckOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }
        
        [Fact]
        public async Task ShouldStartActivitiesForGettingOrchestratorsToAnalyze()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity), Arg.Any<FilterSupervisorsRequest>())
                .Returns(_fixture.CreateMany<Orchestrator>(1).ToList());
            orchestrationContext
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetAllOrchestratorsActivity), null)
                .Returns((_fixture.CreateMany<Orchestrator>(1).ToList(), _fixture.CreateMany<Orchestrator>(1).ToList()));

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received()
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetAllOrchestratorsActivity), null);
            await orchestrationContext.Received()
                .CallActivityAsync<IList<string>>(nameof(GetScannedSupervisorsActivity), null);
            await orchestrationContext.Received()
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity), Arg.Any<FilterSupervisorsRequest>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        public async Task ShouldStartSubOrchestratorForEachAnalysis(int count)
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity), Arg.Any<FilterSupervisorsRequest>())
                .Returns(_fixture.CreateMany<Orchestrator>(count).ToList());
            orchestrationContext
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetAllOrchestratorsActivity), null)
                .Returns((_fixture.CreateMany<Orchestrator>(1).ToList(), _fixture.CreateMany<Orchestrator>(1).ToList()));

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);
            
            //Assert
            await orchestrationContext.Received(count)
                .CallSubOrchestratorAsync(nameof(SingleCompletenessCheckOrchestrator), Arg.Any<SingleCompletenessCheckRequest>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        public async Task ShouldStartDeleteActivityForEachCompletedSupervisor(int count)
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity), Arg.Any<FilterSupervisorsRequest>())
                .Returns(_fixture.CreateMany<Orchestrator>(1).ToList());
            orchestrationContext
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetAllOrchestratorsActivity), null)
                .Returns((_fixture.CreateMany<Orchestrator>(count).ToList(), _fixture.CreateMany<Orchestrator>(1).ToList()));

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received(count)
                .CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}