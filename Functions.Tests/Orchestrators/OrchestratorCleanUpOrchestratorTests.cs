using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Functions.Activities;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class OrchestratorCleanUpOrchestratorTests
    {
        private readonly Fixture _fixture;

        public OrchestratorCleanUpOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldStartActivities()
        {
            //Arrange
            var orchestrationContext = Substitute.For<IDurableOrchestrationContext>();
            orchestrationContext
                .CallActivityAsync<(IList<string>, IList<string>)>(nameof(GetOrchestratorsToPurgeActivity), null)
                .Returns((_fixture.CreateMany<string>(1).ToList(), _fixture.CreateMany<string>(1).ToList()));

            //Act
            var function = new OrchestratorCleanUpOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received()
                .CallActivityAsync<(IList<string>, IList<string>)>(nameof(GetOrchestratorsToPurgeActivity), null);
            await orchestrationContext.Received()
                .CallActivityAsync(nameof(TerminateOrchestratorActivity), Arg.Any<string>());
            await orchestrationContext.Received()
                .CallActivityAsync(nameof(PurgeMultipleOrchestratorsActivity), null);
            await orchestrationContext.Received()
                .CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
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
            var orchestrationContext = Substitute.For<IDurableOrchestrationContext>();
            orchestrationContext
                .CallActivityAsync<(IList<string>, IList<string>)>(nameof(GetOrchestratorsToPurgeActivity), null)
                .Returns((_fixture.CreateMany<string>(count).ToList(), _fixture.CreateMany<string>(count).ToList()));

            //Act
            var function = new OrchestratorCleanUpOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received(count)
                .CallActivityAsync(nameof(TerminateOrchestratorActivity), Arg.Any<string>());
            await orchestrationContext.Received(count)
                .CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), Arg.Any<string>());
        }
    }
}