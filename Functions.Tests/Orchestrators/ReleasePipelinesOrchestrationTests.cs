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
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Orchestrators
{
    public class ReleasePipelinesOrchestrationTests
    {
        private readonly Fixture _fixture;

        public ReleasePipelinesOrchestrationTests()
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
                .Setup(x => x.GetInput<ItemOrchestratorRequest>())
                .Returns(_fixture.Create<ItemOrchestratorRequest>());

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
                    It.IsAny<ReleasePipelinesScanActivityRequest>()))
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
                    It.IsAny<IEnumerable<PreventiveLogItem>>()))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync<IList<ProductionItem>>(
                    nameof(LinkCisToBuildPipelinesActivity), 
                    It.IsAny<(ReleaseDefinition, IList<string>, string)>()))
                .ReturnsAsync(_fixture.Create<IList<ProductionItem>>())
                .Verifiable();

            var environmentConfig = _fixture.Create<EnvironmentConfig>();

            //Act
            var function = new ReleasePipelinesOrchestration(environmentConfig);
            await function.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}