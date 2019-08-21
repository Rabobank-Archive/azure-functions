using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.VstsService.Response;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Orchestrators
{
    public class ReleasePipelinesOrchestrationTests
    {

        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);

            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>());
            
            starter
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => s.Scope == RuleScopes.ReleasePipelines)))
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ReleasePipelinesScanActivity),
                    It.IsAny<RetryOptions>(), 
                    It.IsAny<ReleaseDefinition>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();
            
            starter
                .Setup(x => x.CallActivityWithRetryAsync<List<ReleaseDefinition>>(nameof(ReleaseDefinitionsForProjectActivity),
                    It.IsAny<RetryOptions>(),
                    It.IsAny<Project>()))
                .ReturnsAsync(fixture.CreateMany<ReleaseDefinition>().ToList())
                .Verifiable();
            
            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());

            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => t.scope == RuleScopes.ReleasePipelines)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                    It.Is<LogAnalyticsUploadActivityRequest>(l =>
                    l.PreventiveLogItems.All(p => p.Scope == RuleScopes.ReleasePipelines))))
                .Returns(Task.CompletedTask);
            
            var environmentConfig = fixture.Create<EnvironmentConfig>();

            //Act
            var function = new ReleasePipelinesOrchestration(environmentConfig);
            await function.Run(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}