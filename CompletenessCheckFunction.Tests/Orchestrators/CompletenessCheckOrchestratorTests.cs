using System.Collections.Generic;
using System.Linq;
using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Orchestrators;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;
using Xunit;

namespace CompletenessCheckFunction.Tests.Orchestrators
{
    public class CompletenessCheckOrchestratorTests
    {
        private readonly Fixture _fixture;
        public CompletenessCheckOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }
        //GetSupervisorOrchestrators
        //FilterOnNotYetScanned (later)
        //Foreach
        //GetSubOrchestrators
        //Supervisor.CustomStatus.TotalProjects == GetSubOrchestrators.Where(state=completed).Count
        //PostResultToLogAnalytics
        
        [Fact]
        public async Task ShouldStartActivitiesForGettingOrchestratorsToAnalyze()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext.CallActivityAsync<List<OrchestrationInstance>>(nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                    Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<OrchestrationInstance>(1).ToList());

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.Run(orchestrationContext);

            //Assert
            await orchestrationContext.Received().CallActivityAsync<List<OrchestrationInstance>>(nameof(GetOrchestratorsToVerifyActivity), null);
            await orchestrationContext.Received().CallActivityAsync<List<string>>(nameof(GetCompletedScansFromLogAnalyticsActivity), null);
            await orchestrationContext.Received().CallActivityAsync<List<OrchestrationInstance>>(nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>());
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
            orchestrationContext.CallActivityAsync<List<OrchestrationInstance>>(nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<OrchestrationInstance>(count).ToList());

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.Run(orchestrationContext);
            
            //Assert
            await orchestrationContext.Received(count).CallSubOrchestratorAsync(nameof(SingleAnalysisOrchestrator),
                Arg.Any<SingleAnalysisOrchestratorRequest>());
        }
    }
}
