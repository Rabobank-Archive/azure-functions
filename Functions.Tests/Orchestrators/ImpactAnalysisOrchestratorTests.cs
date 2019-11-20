using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Functions.Activities;
using Functions.Orchestrators;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests.Orchestrators
{
    public class ImpactAnalysisOrchestratorTests
    {
        private readonly Fixture _fixture;
        public ImpactAnalysisOrchestratorTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public async Task ShouldStartAllActivitiesForSoxCi(int count)
        {
            //Arrange
            _fixture.Customize<DeploymentMethod>(x => x
                .With(d => d.IsSoxApplication, true));

            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext
                .CallActivityWithRetryAsync<IList<Response.Project>>(nameof(GetProjectsActivity),
                    _fixture.Create<RetryOptions>(), null)
                .ReturnsForAnyArgs(_fixture.CreateMany<Response.Project>(count).ToList());
            orchestrationContext
                .CallActivityWithRetryAsync<IList<ProductionItem>>(nameof(GetDeploymentMethodsActivity),
                    _fixture.Create<RetryOptions>(), Arg.Any<string>())
                .ReturnsForAnyArgs(_fixture.CreateMany<ProductionItem>(count).ToList());
            orchestrationContext
                .CallActivityWithRetryAsync<IList<Response.Release>>(nameof(GetReleasesActivity),
                    _fixture.Create<RetryOptions>(), _fixture.Create<(string, ProductionItem)>())
                .ReturnsForAnyArgs(_fixture.CreateMany<Response.Release>(count).ToList());
            orchestrationContext
                .CallActivityAsync<bool>(nameof(ScanReleaseActivity),
                    _fixture.Create<Response.Release>())
                .ReturnsForAnyArgs(_fixture.Create<bool>());

            //Act
            var function = new ImpactAnalysisOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received()
                .CallActivityWithRetryAsync<IList<Response.Project>>(nameof(GetProjectsActivity),
                    Arg.Any<RetryOptions>(), Arg.Any<DurableActivityContextBase>());
            await orchestrationContext.Received(count)
                .CallActivityWithRetryAsync<IList<ProductionItem>>(nameof(GetDeploymentMethodsActivity),
                    Arg.Any<RetryOptions>(), Arg.Any<string>());
            await orchestrationContext.Received(count * count)
                .CallActivityWithRetryAsync<IList<Response.Release>>(nameof(GetReleasesActivity),
                    Arg.Any<RetryOptions>(), Arg.Any<(string, ProductionItem)>());
            await orchestrationContext.Received(count * count * count)
                .CallActivityAsync<bool>(nameof(ScanReleaseActivity), Arg.Any<Response.Release>());
            await orchestrationContext.Received(count * count * count)
                 .CallActivityWithRetryAsync(nameof(UploadReleaseLogActivity), Arg.Any<RetryOptions>(),
                    Arg.Any<(Response.Project, Response.Release, ProductionItem, bool)>());
        }

        [Fact]
        public async Task ShouldNotStartScanActivitiesForNonSoxCi()
        {
            //Arrange
            _fixture.Customize<DeploymentMethod>(x => x
                .With(d => d.IsSoxApplication, false));

            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();
            orchestrationContext
                .CallActivityWithRetryAsync<IList<Response.Project>>(nameof(GetProjectsActivity),
                    _fixture.Create<RetryOptions>(), null)
                .ReturnsForAnyArgs(_fixture.CreateMany<Response.Project>(1).ToList());
            orchestrationContext
                .CallActivityWithRetryAsync<IList<ProductionItem>>(nameof(GetDeploymentMethodsActivity),
                    _fixture.Create<RetryOptions>(), Arg.Any<string>())
                .ReturnsForAnyArgs(_fixture.CreateMany<ProductionItem>(1).ToList());

            //Act
            var function = new ImpactAnalysisOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received()
                .CallActivityWithRetryAsync<IList<Response.Project>>(nameof(GetProjectsActivity),
                    Arg.Any<RetryOptions>(), Arg.Any<DurableActivityContextBase>());
            await orchestrationContext.Received()
                .CallActivityWithRetryAsync<IList<ProductionItem>>(nameof(GetDeploymentMethodsActivity),
                    Arg.Any<RetryOptions>(), Arg.Any<string>());
            await orchestrationContext.DidNotReceive()
                .CallActivityWithRetryAsync<IList<Response.Release>>(nameof(GetReleasesActivity),
                    Arg.Any<RetryOptions>(), Arg.Any<(string, ProductionItem)>());
            await orchestrationContext.DidNotReceive()
                .CallActivityAsync<bool>(nameof(ScanReleaseActivity), Arg.Any<Response.Release>());
            await orchestrationContext.DidNotReceive()
                 .CallActivityWithRetryAsync(nameof(UploadReleaseLogActivity), Arg.Any<RetryOptions>(),
                    Arg.Any<(Response.Project, Response.Release, ProductionItem, bool)>());
        }
    }
}