using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Functions.Completeness.Orchestrators;
using Functions.Completeness.Requests;
using Functions.Completeness.Responses;
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
            _fixture.Customize<SimpleDurableOrchestrationStatus>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }
        
        [Fact]
        public async Task ShouldStartActivitiesForGettingOrchestratorsToAnalyze()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext.CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(
                    nameof(FilterAlreadyAnalyzedOrchestratorsActivity), Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<SimpleDurableOrchestrationStatus>(1).ToList());
            orchestrationContext.CallActivityAsync<(IList<SimpleDurableOrchestrationStatus>, IList<SimpleDurableOrchestrationStatus>)>(
                    nameof(GetAllOrchestratorsActivity), null)
                .Returns((_fixture.CreateMany<SimpleDurableOrchestrationStatus>(1).ToList(), 
                    _fixture.CreateMany<SimpleDurableOrchestrationStatus>(1).ToList()));

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received().CallActivityAsync<(IList<SimpleDurableOrchestrationStatus>, 
                IList<SimpleDurableOrchestrationStatus>)>(nameof(GetAllOrchestratorsActivity), null);
            await orchestrationContext.Received().CallActivityAsync<IList<string>>(
                nameof(GetCompletedScansFromLogAnalyticsActivity), null);
            await orchestrationContext.Received().CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(
                nameof(FilterAlreadyAnalyzedOrchestratorsActivity), Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>());
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
            orchestrationContext.CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(
                    nameof(FilterAlreadyAnalyzedOrchestratorsActivity), Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<SimpleDurableOrchestrationStatus>(count).ToList());
            orchestrationContext.CallActivityAsync<(IList<SimpleDurableOrchestrationStatus>, IList<SimpleDurableOrchestrationStatus>)>(
                    nameof(GetAllOrchestratorsActivity), null)
                .Returns((_fixture.CreateMany<SimpleDurableOrchestrationStatus>(1).ToList(),
                    _fixture.CreateMany<SimpleDurableOrchestrationStatus>(1).ToList()));

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);
            
            //Assert
            await orchestrationContext.Received(count).CallSubOrchestratorAsync(nameof(SingleAnalysisOrchestrator),
                Arg.Any<SingleAnalysisOrchestratorRequest>());
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
            orchestrationContext.CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(
                    nameof(FilterAlreadyAnalyzedOrchestratorsActivity), Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<SimpleDurableOrchestrationStatus>(1).ToList());
            orchestrationContext.CallActivityAsync<(IList<SimpleDurableOrchestrationStatus>, IList<SimpleDurableOrchestrationStatus>)>(
                    nameof(GetAllOrchestratorsActivity), null)
                .Returns((_fixture.CreateMany<SimpleDurableOrchestrationStatus>(count).ToList(),
                    _fixture.CreateMany<SimpleDurableOrchestrationStatus>(1).ToList()));

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received(count).CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}
