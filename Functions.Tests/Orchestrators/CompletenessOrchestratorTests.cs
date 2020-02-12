using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Orchestrators;
using Functions.Model;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class CompletenessOrchestratorTests
    {
        private readonly Fixture _fixture;
        public CompletenessOrchestratorTests()
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
            var orchestrationContext = Substitute.For<IDurableOrchestrationContext>();
            orchestrationContext
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity), 
                    Arg.Any<(IList<Orchestrator>, IList<string>)>())
                .Returns(_fixture.CreateMany<Orchestrator>(1).ToList());
            orchestrationContext
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetOrchestratorsToScanActivity), null)
                .Returns((_fixture.CreateMany<Orchestrator>(1).ToList(), _fixture.CreateMany<Orchestrator>(1).ToList()));
            orchestrationContext
                .CallActivityAsync<IList<string>>(nameof(GetScannedSupervisorsActivity), null)
                .Returns(_fixture.CreateMany<string>(1).ToList());

            //Act
            var function = new CompletenessOrchestrator();
            await function.RunAsync(orchestrationContext, Substitute.For<ILogger>());

            //Assert
            await orchestrationContext.Received()
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetOrchestratorsToScanActivity), null);
            await orchestrationContext.Received()
                .CallActivityAsync<IList<string>>(nameof(GetScannedSupervisorsActivity), null);
            await orchestrationContext.Received()
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity), 
                    Arg.Any<(IList<Orchestrator>, IList<string>)>());
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
            var orchestrationContext = Substitute.For<IDurableOrchestrationContext>();
            orchestrationContext
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity), 
                    Arg.Any<(IList<Orchestrator>, IList<string>)>())
                .Returns(_fixture.CreateMany<Orchestrator>(count).ToList());
            orchestrationContext
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetOrchestratorsToScanActivity), null)
                .Returns((_fixture.CreateMany<Orchestrator>(1).ToList(), _fixture.CreateMany<Orchestrator>(1).ToList()));
            orchestrationContext
                .CallActivityAsync<IList<string>>(nameof(GetScannedSupervisorsActivity), null)
                .Returns(_fixture.CreateMany<string>(1).ToList());

            //Act
            var function = new CompletenessOrchestrator();
            await function.RunAsync(orchestrationContext, Substitute.For<ILogger>());

            //Assert
            await orchestrationContext.Received(count)
                .CallSubOrchestratorAsync(nameof(SingleCompletenessOrchestrator), 
                    Arg.Any<(Orchestrator, IList<Orchestrator>)>());
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
            var orchestrationContext = Substitute.For<IDurableOrchestrationContext>();
            orchestrationContext
                .CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(nameof(GetOrchestratorsToScanActivity), null)
                .Returns((_fixture.CreateMany<Orchestrator>(1).ToList(), _fixture.CreateMany<Orchestrator>(1).ToList()));
            orchestrationContext
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity),
                    Arg.Any<(IList<Orchestrator>, IList<string>)>())
                .Returns(_fixture.CreateMany<Orchestrator>(count).ToList());
            orchestrationContext
                .CallActivityAsync<IList<string>>(nameof(GetScannedSupervisorsActivity), null)
                .Returns(_fixture.CreateMany<string>(1).ToList());

            //Act
            var function = new CompletenessOrchestrator();
            await function.RunAsync(orchestrationContext, Substitute.For<ILogger>());

            //Assert
            await orchestrationContext.Received(count)
                .CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}