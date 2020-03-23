using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
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
    public class ReleasePipelinesOrchestratorTests
    {
        private readonly Fixture _fixture;

        public ReleasePipelinesOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            _fixture.Customize<Response.ReleaseDefinition>(f => f
                 .With(r => r.Id, "1"));

            var mocks = new MockRepository(MockBehavior.Strict);

            var starter = mocks.Create<IDurableOrchestrationContext>();
            starter
                .Setup(x => x.GetInput<(Response.Project,  DateTime)>())
                .Returns(_fixture.Create<(Response.Project,  DateTime)>());

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(ScanReleasePipelinesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<(Response.Project, Response.ReleaseDefinition)>()))
                .ReturnsAsync(_fixture.Create<ItemExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<List<Response.ReleaseDefinition>>(
                    nameof(GetReleasePipelinesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<string>()))
                .ReturnsAsync(_fixture.CreateMany<Response.ReleaseDefinition>().ToList())
                .Verifiable();

            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());

            starter
                .Setup(x => x.CallActivityAsync<object>(nameof(UploadExtensionDataActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => 
                    t.scope == RuleScopes.ReleasePipelines)))
                .Returns(Task.FromResult<object>(null));
            
            var environmentConfig = _fixture.Create<EnvironmentConfig>();

            //Act
            var function = new ReleasePipelinesOrchestrator(environmentConfig);
            await function.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}