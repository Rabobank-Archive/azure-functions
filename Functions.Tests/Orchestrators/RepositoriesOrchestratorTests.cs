using System;
using System.Collections.Generic;
using AutoFixture;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using Xunit;
using Task = System.Threading.Tasks.Task;

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
                .Setup(x => x.GetInput<(Project, List<ProductionItem>, DateTime)>())
                .Returns(fixture.Create<(Project, List<ProductionItem>, DateTime)>());

            starter
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s =>
                    s.Scope == RuleScopes.Repositories)))
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(ScanRepositoriesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<(Project, Repository,
                    string)>()))
                .ReturnsAsync(fixture.Create<ItemExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<IEnumerable<Repository>>(
                    nameof(GetRepositoriesAndPoliciesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<Project>()))
                .ReturnsAsync((fixture.CreateMany<Repository>()))
                .Verifiable();

            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());

            starter
                .Setup(x => x.CallActivityAsync<object>(nameof(UploadExtensionDataActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t =>
                    t.scope == RuleScopes.Repositories)))
                .Returns(Task.FromResult<object>(null));

            starter
                .Setup(x => x.CallActivityAsync<object>(nameof(UploadPreventiveRuleLogsActivity),
                    It.IsAny<IEnumerable<PreventiveRuleLogItem>>()))
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