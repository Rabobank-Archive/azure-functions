using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AzureDevOps.Compliance.Rules;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using Xunit;
using System.Threading.Tasks;

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
                .Setup(x => x.GetInput<(Response.Project, DateTime)>())
                .Returns(fixture.Create<(Response.Project, DateTime)>());

            starter.Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                nameof(ScanBuildPipelinesActivity), It.IsAny<RetryOptions>(), 
                    It.IsAny<(Response.Project, Response.BuildDefinition)>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();
            
            starter
                .Setup(x => x.CallActivityWithRetryAsync<List<Response.BuildDefinition>>(
                    nameof(GetBuildPipelinesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<string>()))
                .ReturnsAsync(fixture.CreateMany<Response.BuildDefinition>().ToList())
                .Verifiable();
            
            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());
                
            starter
                .Setup(x => x.CallActivityAsync<object>(nameof(UploadExtensionDataActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => 
                    t.scope == RuleScopes.BuildPipelines)))
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