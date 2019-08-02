using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Orchestrators;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using Xunit;

namespace CompletenessCheckFunction.Tests.Orchestrators
{
    public class SingleAnalysisOrchestratorTests
    {
        private readonly Fixture _fixture;
        public SingleAnalysisOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldCallActivities()
        {
            // Arrange
            var context = Substitute.For<DurableOrchestrationContextBase>();

            _fixture.Customize<OrchestrationInstance>(o =>
                o.With(i => i.CustomStatus, new SupervisorOrchestrationStatus {TotalProjectCount = 3}));
            context.GetInput<SingleAnalysisOrchestratorRequest>().Returns(new SingleAnalysisOrchestratorRequest
                {InstanceToAnalyze = _fixture.Create<OrchestrationInstance>()});
            
            context.CallActivityAsync<List<OrchestrationInstance>>(nameof(FilterOrchestratorsForParentIdActivity),
                    Arg.Any<FilterOrchestratorsForParentIdActivityRequest>())
                .Returns(_fixture.CreateMany<OrchestrationInstance>().ToList());
            
            // Act
            var fun = new SingleAnalysisOrchestrator();
            await fun.RunAsync(context);
            
            // Assert
            await context.Received().CallActivityAsync<List<OrchestrationInstance>>(
                nameof(FilterOrchestratorsForParentIdActivity), Arg.Any<FilterOrchestratorsForParentIdActivityRequest>());
            await context.Received().CallActivityAsync(nameof(UploadAnalysisResultToLogAnalyticsActivity),
                Arg.Any<UploadAnalysisResultToLogAnalyticsActivityRequest>());
        }
        
        [Fact]
        public async Task ShouldNotAnalyzeIfNoTotalProjectCount()
        {
            // Arrange
            var context = Substitute.For<DurableOrchestrationContextBase>();
            context.GetInput<SingleAnalysisOrchestratorRequest>().Returns(new SingleAnalysisOrchestratorRequest
                {InstanceToAnalyze = _fixture.Create<OrchestrationInstance>()});
            
            // Act
            var fun = new SingleAnalysisOrchestrator();
            await fun.RunAsync(context);
            
            // Assert
            await context.DidNotReceive().CallActivityAsync<List<OrchestrationInstance>>(nameof(GetCompletedOrchestratorsWithNameActivity), "ProjectScanOrchestration");
        }
    }
}