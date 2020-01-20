using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Orchestrators
{
    public class BuildPipelinesOrchestratorTests
    {
        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);

            var starter = mocks.Create<IDurableOrchestrationContext>();
            starter
                .Setup(x => x.GetInput<(Project, List<ProductionItem>)>())
                .Returns(fixture.Create<(Project, List<ProductionItem>)>());

            starter
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => 
                    s.Scope == RuleScopes.BuildPipelines)))
                .Verifiable();

            starter.Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                nameof(ScanBuildPipelinesActivity), It.IsAny<RetryOptions>(), 
                    It.IsAny<(Project, BuildDefinition, string)>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();
            
            starter
                .Setup(x => x.CallActivityWithRetryAsync<List<BuildDefinition>>(
                    nameof(GetBuildPipelinesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<string>()))
                .ReturnsAsync(fixture.CreateMany<BuildDefinition>().ToList())
                .Verifiable();
            
            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());
                
            starter
                .Setup(x => x.CallActivityAsync<object>(nameof(UploadExtensionDataActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => 
                    t.scope == RuleScopes.BuildPipelines)))
                .Returns(Task.FromResult<object>(null));

            starter
                .Setup(x => x.CallActivityAsync<object>(nameof(UploadPreventiveRuleLogsActivity),
                    It.IsAny<IEnumerable<PreventiveRuleLogItem>>()))
                .Returns(Task.FromResult<object>(null));

            var environmentConfig = fixture.Create<EnvironmentConfig>();

            //Act
            var function = new BuildPipelinesOrchestrator(environmentConfig);
            await function.RunAsync(starter.Object);
            
            //Assert           
            mocks.VerifyAll();
        }
    }
}