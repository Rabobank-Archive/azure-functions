using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Functions.Completeness.Orchestrators;
using Functions.Completeness.Requests;
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
            _fixture.Customize<DurableOrchestrationStatus>(s => s
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new{ }))
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }
        
        [Fact]
        public async Task ShouldStartActivitiesForGettingOrchestratorsToAnalyze()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                    Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(1).ToList());
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(
                    nameof(GetOrchestratorsByNameActivity), "ProjectScanSupervisor")
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(1).ToList());

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received().CallActivityAsync<List<DurableOrchestrationStatus>>(nameof(GetOrchestratorsByNameActivity), "ProjectScanSupervisor");
            await orchestrationContext.Received().CallActivityAsync<List<string>>(nameof(GetCompletedScansFromLogAnalyticsActivity), null);
            await orchestrationContext.Received().CallActivityAsync<List<DurableOrchestrationStatus>>(nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>());
            await orchestrationContext.Received().CallActivityAsync<List<DurableOrchestrationStatus>>(nameof(GetOrchestratorsByNameActivity), "ProjectScanOrchestration");
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
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(count).ToList());
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(
                    nameof(GetOrchestratorsByNameActivity), "ProjectScanSupervisor")
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(1).ToList());

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
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                Arg.Any<FilterAlreadyAnalyzedOrchestratorsActivityRequest>())
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(count).ToList());
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(
                    nameof(GetOrchestratorsByNameActivity), "ProjectScanSupervisor")
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(count).ToList());

            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received(count).CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}
