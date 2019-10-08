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
            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }

        [Fact]
        public async Task ShouldCallActivities()
        {
            // Arrange
            var context = Substitute.For<DurableOrchestrationContextBase>();

            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.CustomStatus, JToken.FromObject(JToken.FromObject(
                    new SupervisorOrchestrationStatus { TotalProjectCount = 3 }))));

            context
                .GetInput<SingleCompletenessCheckRequest>()
                .Returns(_fixture.Create<SingleCompletenessCheckRequest>());
            context
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity), Arg.Any<SingleCompletenessCheckRequest>())
                .Returns(_fixture.CreateMany<Orchestrator>().ToList());

            // Act
            var fun = new SingleCompletenessCheckOrchestrator();
            await fun.RunAsync(context);

            // Assert
            await context.Received()
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity), Arg.Any<SingleCompletenessCheckRequest>());
            await context.Received()
                .CallActivityAsync<CompletenessReport>(nameof(CreateCompletenessReportActivity), Arg.Any<CreateCompletenessReportRequest>());
            await context.Received()
                .CallActivityAsync(nameof(UploadCompletenessLogsActivity), Arg.Any<CompletenessReport>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        public async Task ShouldStartDeleteActivityForEachCompletedOrchestrator(int count)
        {
            //Arrange
            var context = Substitute.For<DurableOrchestrationContextBase>();

            _fixture.Customize<Orchestrator>(s => s
                .With(d => d.RuntimeStatus, OrchestrationRuntimeStatus.Completed)
                .With(d => d.CustomStatus, JToken.FromObject(JToken.FromObject(
                    new SupervisorOrchestrationStatus { TotalProjectCount = 1 }))));

            context
                .GetInput<SingleCompletenessCheckRequest>()
                .Returns(_fixture.Create<SingleCompletenessCheckRequest>());
            context
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity), Arg.Any<SingleCompletenessCheckRequest>())
                .Returns(_fixture.CreateMany<Orchestrator>(count).ToList());

            // Act
            var fun = new SingleCompletenessCheckOrchestrator();
            await fun.RunAsync(context);

            //Assert
            await context.Received(count)
                .CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}