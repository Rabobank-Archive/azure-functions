using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GlobalPermissionsScanProjectActivityTests
    {
        [Fact]
        public async Task RunShouldCallIProjectRuleEvaluate()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var rule = new Mock<IProjectRule>();
            rule
                .Setup(x => x.Evaluate(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var ruleSets = new Mock<IRulesProvider>();
            ruleSets
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new[] {rule.Object, rule.Object});

            //Act
            var fun = new GlobalPermissionsScanProjectActivity(
                fixture.Create<IVstsRestClient>(),
                fixture.Create<EnvironmentConfig>(),
                ruleSets.Object);

            await fun.RunAsActivity(
                fixture.Create<Response.Project>());

            //Assert
            rule.Verify(x => x.Evaluate(It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task
            GivenGlobalPermissionsAreScanned_WhenReportsArePutInExtensionDataStorage_ThenItShouldHaveReconcileUrls()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();
            var dummyproj = fixture.Create<Response.Project>();
            var clientMock = new Mock<IVstsRestClient>();

            var rule = new Mock<IProjectRule>();
            rule
                .As<IProjectReconcile>()
                .Setup(x => x.Impact)
                .Returns(new[] {"just some action"});

            var rulesProvider = new Mock<IRulesProvider>();
            rulesProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new[] {rule.Object});

            //Act
            var fun = new GlobalPermissionsScanProjectActivity(
                clientMock.Object,
                config,
                rulesProvider.Object);
            var result = await fun.RunAsActivity(
                dummyproj);

            var ruleName = rule.Object.GetType().Name;

            // Assert
            result.Reports.ShouldContain(r => r.Reconcile != null &&
                                              r.Reconcile.Url ==
                                              $"https://{config.FunctionAppHostname}/api/reconcile/{config.Organization}/{dummyproj.Name}/globalpermissions/{ruleName}" &&
                                              r.Reconcile.Impact.Any());
            result.RescanUrl.ShouldNotBeNull();
        }
    }
}