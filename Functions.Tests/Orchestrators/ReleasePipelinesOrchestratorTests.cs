using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

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
            _fixture.Customize<ReleaseDefinition>(f => f
                 .With(r => r.Id, "1"));
            _fixture.Customize<ProductionItem>(f => f
                .With(p => p.ItemId, "1"));

            var mocks = new MockRepository(MockBehavior.Strict);

            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<(Project, List<ProductionItem>)>())
                .Returns(_fixture.Create<(Project, List<ProductionItem>)>());

            starter
                .Setup(x => x.InstanceId)
                .Returns(_fixture.Create<string>());

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => 
                    s.Scope == RuleScopes.ReleasePipelines)))
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(ScanReleasePipelinesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<(Project, ReleaseDefinition, IEnumerable<ProductionItem>)>()))
                .ReturnsAsync(_fixture.Create<ItemExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<List<ReleaseDefinition>>(
                    nameof(GetReleasePipelinesActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<string>()))
                .ReturnsAsync(_fixture.CreateMany<ReleaseDefinition>().ToList())
                .Verifiable();

            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());

            starter
                .Setup(x => x.CallActivityAsync(nameof(UploadExtensionDataActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => 
                    t.scope == RuleScopes.ReleasePipelines)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(UploadPreventiveRuleLogsActivity),
                    It.IsAny<IEnumerable<PreventiveRuleLogItem>>()))
                .Returns(Task.CompletedTask);

            var environmentConfig = _fixture.Create<EnvironmentConfig>();

            //Act
            var function = new ReleasePipelinesOrchestrator(environmentConfig);
            var result = await function.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
            result.ShouldNotBeNull();
        }
    }
}