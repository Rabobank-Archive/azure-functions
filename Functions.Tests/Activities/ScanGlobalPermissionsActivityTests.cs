using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.Rules.Security;
using Response = SecurePipelineScan.VstsService.Response;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Functions.Helpers;

namespace Functions.Tests.Activities
{
    public class ScanGlobalPermissionsActivityTests
    {
        [Fact]
        public async Task RunShouldCallIProjectRuleEvaluate()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var rule = new Mock<IProjectRule>();
            rule
                .Setup(x => x.EvaluateAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var project = fixture.Create<Response.Project>();
            var ciIdentifiers = fixture.Create<string>();

            var soxLookup = fixture.Create<SoxLookup>();

            //Act
            var fun = new ScanGlobalPermissionsActivity(
                fixture.Create<EnvironmentConfig>(),
                new[] { rule.Object, rule.Object }, soxLookup);

            await fun.RunAsync((project, ciIdentifiers));

            //Assert
            rule.Verify(x => x.EvaluateAsync(It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task
            GivenGlobalPermissionsAreScanned_WhenReportsArePutInExtensionDataStorage_ThenItShouldHaveReconcileUrls()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();

            var project = fixture.Create<Response.Project>();
            var ciIdentifiers = fixture.Create<string>();

            var rule = new Mock<IProjectRule>();
            rule
                .As<IProjectReconcile>()
                .Setup(x => x.Impact)
                .Returns(new[] { "just some action" });

            var soxLookup = fixture.Create<SoxLookup>();

            //Act
            var fun = new ScanGlobalPermissionsActivity(
                config,
                new[] { rule.Object }, soxLookup);
            var result = await fun.RunAsync((project, ciIdentifiers));

            var ruleName = rule.Object.GetType().Name;

            // Assert
            result.Rules.ShouldContain(r => r.Reconcile != null);
            result.Rules.ShouldContain(r => r.Reconcile.Impact.Any());
            result.Rules.ShouldContain(r => r.Reconcile.Url == new Uri($"https://{config.FunctionAppHostname}" +
                $"/api/reconcile/{config.Organization}/{project.Id}/globalpermissions/{ruleName}"));
        }
    }
}