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
using System;

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
            const string instanceId = "supervisorId:projectId:scope";

            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<ItemOrchestratorRequest>())
                .Returns(fixture.Create<ItemOrchestratorRequest>());

            starter
                .Setup(x => x.InstanceId)
                .Returns(instanceId);

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => 
                    s.Scope == RuleScopes.GlobalPermissions)))
                .Verifiable();

            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(GlobalPermissionsScanActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<ItemOrchestratorRequest>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(d =>
                    d.scope == RuleScopes.GlobalPermissions)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity), 
                    It.Is<LogAnalyticsUploadActivityRequest>(l => l.PreventiveLogItems.All(p => 
                    p.Scope == RuleScopes.GlobalPermissions && p.ScanId == "supervisorId"))))
                .Returns(Task.CompletedTask);

            //Act
            var function = new GlobalPermissionsOrchestration(fixture.Create<EnvironmentConfig>());
            await function.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}