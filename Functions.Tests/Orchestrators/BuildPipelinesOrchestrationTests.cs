using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using System.Threading.Tasks;
using AzDoCompliancy.CustomStatus;
using Functions.Helpers;
using NSubstitute;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests.Orchestrators
{
    public class BuildPipelinesOrchestrationTests
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
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => s.Scope == RuleScopes.BuildPipelines)))
                .Verifiable();

            starter.Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(nameof(BuildPipelinesScanActivity),
                    It.IsAny<RetryOptions>(), 
                    It.IsAny<Response.BuildDefinition>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();
            
            starter
                .Setup(x => x.CallActivityWithRetryAsync<List<Response.BuildDefinition>>(nameof(BuildDefinitionsActivity),
                    It.IsAny<RetryOptions>(),
                    It.IsAny<Response.Project>()))
                .ReturnsAsync(fixture.CreateMany<Response.BuildDefinition>().ToList())
                .Verifiable();

            
            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());
                
            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => t.scope == RuleScopes.BuildPipelines)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                    It.Is<LogAnalyticsUploadActivityRequest>(l =>
                    l.PreventiveLogItems.All(p => p.Scope == RuleScopes.BuildPipelines))))
                .Returns(Task.CompletedTask);

            var environmentConfig = fixture.Create<EnvironmentConfig>();

            //Act
            var function = new BuildPipelinesOrchestration(environmentConfig);
            await function.Run(starter.Object);
            
            //Assert           
            mocks.VerifyAll();
        }
    }
}