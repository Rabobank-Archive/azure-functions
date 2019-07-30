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
    public class GlobalPermissionsOrchestrationTests
    {

        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            const string instanceId = "abc";


            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<Response.Project>())
                .Returns(fixture.Create<Response.Project>());

            starter
                .Setup(x => x.InstanceId)
                .Returns(instanceId);

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => s.Scope == RuleScopes.GlobalPermissions)))
                .Verifiable();

            starter
                .Setup(x => x.CallActivityAsync<GlobalPermissionsExtensionData>(nameof(GlobalPermissionsScanProjectActivity), It.IsAny<Response.Project>()))
                .ReturnsAsync(fixture.Create<GlobalPermissionsExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataGlobalPermissionsUploadActivity),
                    It.Is<(GlobalPermissionsExtensionData data, string scope)>(d =>
                        d.scope == RuleScopes.GlobalPermissions)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(
                    nameof(LogAnalyticsUploadActivity), 
                    It.Is<LogAnalyticsUploadActivityRequest>(
                        l =>
                            l.PreventiveLogItems.All(
                                p =>
                                    p.Scope == RuleScopes.GlobalPermissions && p.ScanId == instanceId)
                            )
                    )
                )
                .Returns(Task.CompletedTask);

            //Act
            await GlobalPermissionsOrchestration.Run(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}