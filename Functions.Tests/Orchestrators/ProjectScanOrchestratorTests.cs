using System;
using System.Collections.Generic;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
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

            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<(Project, string)>())
                .Returns((fixture.Create<Project>(), "unknownScope"));

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
                    It.IsAny<Project>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
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
                    It.IsAny<Project>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
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
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Once);
            starter
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
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
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Never);
            starter
                .Verify(x => x.CallSubOrchestratorAsync(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()), Times.Once);
        }

        private Mock<DurableOrchestrationContextBase> CreateStarter(Fixture fixture, MockRepository mocks, string scope)
        {
            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<(Project, string)>())
                .Returns((fixture.Create<Project>(), scope));
            starter
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());
            starter
                .Setup(x => x.CallActivityWithRetryAsync<IList<ProductionItem>>(
                    nameof(GetDeploymentMethodsActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<Project>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();
            starter
                .Setup(x => x.CallSubOrchestratorAsync(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(),
                    It.IsAny<(Project, IList<ProductionItem>)>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            return starter;
        }
    }
}