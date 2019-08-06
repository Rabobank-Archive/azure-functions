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
    public class OrchestratorCleanUpOrchestratorTests
    {
        private readonly Fixture _fixture;
        public OrchestratorCleanUpOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
            _fixture.Customize<DurableOrchestrationStatus>(s => s
                .With(d => d.Input, JToken.FromObject(new { }))
                .With(d => d.Output, JToken.FromObject(new { }))
                .With(d => d.CustomStatus, JToken.FromObject(new CustomStatusBase())));
        }

        [Fact]
        public async Task ShouldStartActivities()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(
                    nameof(GetOrchestratorsToPurgeActivity), null)
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(1).ToList());

            //Act
            var function = new OrchestratorCleanUpOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received().CallActivityAsync<List<DurableOrchestrationStatus>>(nameof(GetOrchestratorsToPurgeActivity), null);
            await orchestrationContext.Received().CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
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
            orchestrationContext.CallActivityAsync<List<DurableOrchestrationStatus>>(
                    nameof(GetOrchestratorsToPurgeActivity), null)
                .Returns(_fixture.CreateMany<DurableOrchestrationStatus>(count).ToList());
        
            //Act
            var function = new OrchestratorCleanUpOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received(count).CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}
