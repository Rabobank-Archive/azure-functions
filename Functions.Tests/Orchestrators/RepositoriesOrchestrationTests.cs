using System.Linq;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using System.Threading.Tasks;
using AzDoCompliancy.CustomStatus;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests.Orchestrators
{
    public class RepositoriesOrchestrationTests
    {

        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);

            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<Response.Project>())
                .Returns(fixture.Create<Response.Project>());

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => s.Scope == RuleScopes.Repositories)))
                .Verifiable();

            starter
                .Setup(x => x.CallActivityAsync<ItemsExtensionData>(nameof(RepositoriesScanActivity), It.IsAny<Response.Project>()))
                .ReturnsAsync(fixture.Create<ItemsExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => t.scope == RuleScopes.Repositories)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                    It.Is<LogAnalyticsUploadActivityRequest>(l =>
                        l.PreventiveLogItems.All(p => p.Scope == RuleScopes.Repositories))))
                .Returns(Task.CompletedTask);

            //Act
            await RepositoriesOrchestration.Run(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}