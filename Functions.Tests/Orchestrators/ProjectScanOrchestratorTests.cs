using System;
using System.Collections.Generic;
using AutoFixture;
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
    public class ProjectScanOrchestratorTests
    {
        [Fact]
        public async Task RunAsync_WithoutScope_AllScopesShouldBeInvoked()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            var starter = CreateStarter(fixture, mocks, null);

            //Act
            var fun = new ProjectScanOrchestrator();
            await fun.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }

        [Fact]
        public async Task RunAsync_WithInvalidScope_NoScopesShouldBeInvoked()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);

            var starter = mocks.Create<IDurableOrchestrationContext>();
            starter
                .Setup(x => x.GetInput<(Project, string, DateTime)>())
                .Returns((fixture.Create<Project>(), "unknownScope", fixture.Create<DateTime>()));

            starter
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());

            //Act
            var fun = new ProjectScanOrchestrator();

            //Assert           
            await Assert.ThrowsAsync<InvalidOperationException>(() => fun.RunAsync(starter.Object));
        }

        [Fact]
        public async Task
            RunAsync_WithGlobalPermissionsScope_GetDeploymentMethodsActivityAndGlobalPermissionsOrchestratorShouldBeInvoked()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            var starter = CreateStarter(fixture, mocks, RuleScopes.GlobalPermissions);

            //Act
            var fun = new ProjectScanOrchestrator();
            await fun.RunAsync(starter.Object);

            //Assert     
            starter
                .Verify(x => x.CallActivityWithRetryAsync<IList<ProductionItem>>(
                    nameof(GetDeploymentMethodsActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<string>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
        }


        [Fact]
        public async Task
            RunAsync_WithReleasePipelinesScope_GetDeploymentMethodsActivityAndReleasePipelinesOrchestratorShouldBeInvoked()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            var starter = CreateStarter(fixture, mocks, RuleScopes.ReleasePipelines);

            //Act
            var fun = new ProjectScanOrchestrator();
            await fun.RunAsync(starter.Object);

            //Assert     
            starter
                .Verify(x => x.CallActivityWithRetryAsync<IList<ProductionItem>>(
                    nameof(GetDeploymentMethodsActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<string>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WithBuildPipelinesScope_BuildPipelinesOrchestratorShouldBeInvoked()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            var starter = CreateStarter(fixture, mocks, RuleScopes.BuildPipelines);

            //Act
            var fun = new ProjectScanOrchestrator();
            await fun.RunAsync(starter.Object);

            //Assert     
            starter
                .Verify(x => x.CallActivityWithRetryAsync<IList<ProductionItem>>(
                    nameof(GetDeploymentMethodsActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<Project>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WithRepositoriesScope_RepositoriesOrchestratorShouldBeInvoked()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            var starter = CreateStarter(fixture, mocks, RuleScopes.Repositories);

            //Act
            var fun = new ProjectScanOrchestrator();
            await fun.RunAsync(starter.Object);

            //Assert     
            starter
                .Verify(x => x.CallActivityWithRetryAsync<IList<ProductionItem>>(
                    nameof(GetDeploymentMethodsActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<Project>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<object>(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()), Times.Once);
        }

        private Mock<IDurableOrchestrationContext> CreateStarter(Fixture fixture, MockRepository mocks, string scope)
        {
            var starter = mocks.Create<IDurableOrchestrationContext>();
            starter
                .Setup(x => x.GetInput<(Project, string, DateTime)>())
                .Returns((fixture.Create<Project>(), scope, fixture.Create<DateTime>()));
            starter
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());
            starter
                .Setup(x => x.CallActivityWithRetryAsync<IList<ProductionItem>>(
                    nameof(GetDeploymentMethodsActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<string>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync<object>(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync<object>(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>, DateTime)>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();
            return starter;
        }
    }
}