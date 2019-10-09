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
using SecurePipelineScan.Rules.Security;
using System.Collections.Generic;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests.Orchestrators
{
    public class GlobalPermissionsOrchestratorTests
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
                .Setup(x => x.GetInput<(Response.Project, List<ProductionItem>)>())
                .Returns(fixture.Create<(Response.Project, List<ProductionItem>)>());

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
                    nameof(ScanGlobalPermissionsActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<(Response.Project, List<ProductionItem>)>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityAsync(nameof(UploadExtensionDataActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(d =>
                    d.scope == RuleScopes.GlobalPermissions)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(UploadPreventiveRuleLogsActivity),
                    It.IsAny<IEnumerable<PreventiveRuleLogItem>>()))
                .Returns(Task.CompletedTask);

            //Act
            var function = new GlobalPermissionsOrchestrator(fixture.Create<EnvironmentConfig>());
            await function.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}