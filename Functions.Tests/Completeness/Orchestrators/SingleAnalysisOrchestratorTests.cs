using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Completeness.Activities;
using Functions.Completeness.Model;
using Functions.Completeness.Orchestrators;
using Functions.Completeness.Requests;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Completeness.Orchestrators
{
    public class SingleAnalysisOrchestratorTests
    {
        private readonly Fixture _fixture;
        public SingleAnalysisOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<SimpleDurableOrchestrationStatus>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }

        [Fact]
        public async Task ShouldCallActivities()
        {
            // Arrange
            var context = Substitute.For<DurableOrchestrationContextBase>();

            _fixture.Customize<SimpleDurableOrchestrationStatus>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(JToken.FromObject(
                    new SupervisorOrchestrationStatus { TotalProjectCount = 3 }))));

            context.GetInput<SingleAnalysisOrchestratorRequest>().Returns(_fixture.Create<SingleAnalysisOrchestratorRequest>());
            context.CallActivityAsync<int?>(nameof(GetTotalProjectCountFromSupervisorOrchestrationStatusActivity),
                Arg.Any<SimpleDurableOrchestrationStatus>()).Returns(1);
            context.CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(nameof(FilterOrchestratorsForParentIdActivity),
                    Arg.Any<FilterOrchestratorsForParentIdActivityRequest>())
                .Returns(_fixture.CreateMany<SimpleDurableOrchestrationStatus>().ToList());

            // Act
            var fun = new SingleAnalysisOrchestrator();
            await fun.RunAsync(context);

            // Assert
            await context.Received().CallActivityAsync<int?>(
                nameof(GetTotalProjectCountFromSupervisorOrchestrationStatusActivity),
                Arg.Any<SimpleDurableOrchestrationStatus>());
            await context.Received().CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(
                nameof(FilterOrchestratorsForParentIdActivity), Arg.Any<FilterOrchestratorsForParentIdActivityRequest>());
            await context.Received().CallActivityAsync<CompletenessAnalysisResult>
                (nameof(CreateAnalysisResultActivity), Arg.Any<CreateAnalysisResultActivityRequest>());
            await context.Received().CallActivityAsync(nameof(UploadAnalysisResultToLogAnalyticsActivity),
                Arg.Any<CompletenessAnalysisResult>());
        }

        [Fact]
        public async Task ShouldNotAnalyzeIfNoTotalProjectCount()
        {
            // Arrange
            var context = Substitute.For<DurableOrchestrationContextBase>();
            context.GetInput<SingleAnalysisOrchestratorRequest>()
                .Returns(_fixture.Create<SingleAnalysisOrchestratorRequest>());
            context.CallActivityAsync<int?>(nameof(GetTotalProjectCountFromSupervisorOrchestrationStatusActivity),
                Arg.Any<SimpleDurableOrchestrationStatus>()).Returns((int?)null);

            // Act
            var fun = new SingleAnalysisOrchestrator();
            await fun.RunAsync(context);

            // Assert
            await context.DidNotReceive().CallActivityAsync(nameof(UploadAnalysisResultToLogAnalyticsActivity),
                Arg.Any<CompletenessAnalysisResult>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        public async Task ShouldStartDeleteActivityForEachOrchestrator(int count)
        {
            //Arrange
            var context = Substitute.For<DurableOrchestrationContextBase>();

            _fixture.Customize<SimpleDurableOrchestrationStatus>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(JToken.FromObject(new SupervisorOrchestrationStatus { TotalProjectCount = 1 }))));

            context.GetInput<SingleAnalysisOrchestratorRequest>()
                .Returns(_fixture.Create<SingleAnalysisOrchestratorRequest>());
            context.CallActivityAsync<int?>(nameof(GetTotalProjectCountFromSupervisorOrchestrationStatusActivity),
                    Arg.Any<SimpleDurableOrchestrationStatus>())
                .Returns(1);
            context.CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(nameof(FilterOrchestratorsForParentIdActivity),
                    Arg.Any<FilterOrchestratorsForParentIdActivityRequest>())
                .Returns(_fixture.CreateMany<SimpleDurableOrchestrationStatus>(count).ToList());

            // Act
            var fun = new SingleAnalysisOrchestrator();
            await fun.RunAsync(context);

            //Assert
            await context.Received(count).CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}