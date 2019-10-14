using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class SingleCompletenessOrchestratorTests
    {
        private readonly Fixture _fixture;

        public SingleCompletenessOrchestratorTests()
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
                .GetInput<(Orchestrator, IList<Orchestrator>)>()
                .Returns(_fixture.Create<(Orchestrator, IList<Orchestrator>)>());
            context
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity), 
                    Arg.Any<(Orchestrator, IList<Orchestrator>)>())
                .Returns(_fixture.CreateMany<Orchestrator>().ToList());

            // Act
            var fun = new SingleCompletenessOrchestrator();
            await fun.RunAsync(context);

            // Assert
            await context.Received()
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity), 
                    Arg.Any<(Orchestrator, IList<Orchestrator>)>());
            await context.Received()
                .CallActivityAsync<CompletenessLogItem>(nameof(CreateCompletenessLogItemActivity), 
                    Arg.Any<(DateTime, Orchestrator, IList<Orchestrator>)>());
            await context.Received()
                .CallActivityAsync(nameof(UploadCompletenessLogsActivity), Arg.Any<CompletenessLogItem>());
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
                .GetInput<(Orchestrator, IList<Orchestrator>)>()
                .Returns(_fixture.Create<(Orchestrator, IList<Orchestrator>)>());
            context
                .CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity), 
                    Arg.Any<(Orchestrator, IList<Orchestrator>)>())
                .Returns(_fixture.CreateMany<Orchestrator>(count).ToList());

            // Act
            var fun = new SingleCompletenessOrchestrator();
            await fun.RunAsync(context);

            //Assert
            await context.Received(count)
                .CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}