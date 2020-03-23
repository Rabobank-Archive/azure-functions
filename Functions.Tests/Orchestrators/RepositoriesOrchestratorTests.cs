using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AzureDevOps.Compliance.Rules;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class RepositoriesOrchestratorTests
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

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(ScanRepositoriesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<(Response.Project, Response.Repository)>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<IEnumerable<Response.Repository>>(
                    nameof(GetRepositoriesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<Response.Project>()))
                .ReturnsAsync((fixture.CreateMany<Response.Repository>()))
                .Verifiable();

            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());

            starter
                .Setup(x => x.CallActivityAsync<object>(nameof(UploadExtensionDataActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t =>
                    t.scope == RuleScopes.Repositories)))
                .Returns(Task.FromResult<object>(null));

            var environmentConfig = fixture.Create<EnvironmentConfig>();

            //Act
            var function = new RepositoriesOrchestrator(environmentConfig);
            await function.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}