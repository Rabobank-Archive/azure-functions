using System;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using VstsLogAnalytics.Client;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalyticsFunction.GlobalPermissionsScan;
using VstsLogAnalyticsFunction.Model;

namespace VstsLogAnalyticsFunction.Tests.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectActivityTests
    {
        [Fact]
        public async Task RunShouldCallIProjectRuleEvaluate()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var iLoggerMock = new Mock<ILogger>();

            var rule = new Mock<IProjectRule>();
            rule
                .Setup(x => x.Evaluate(It.IsAny<string>()))
                .Returns(true);

            var ruleSets = new Mock<IRulesProvider>();
            ruleSets
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new [] { rule.Object });


            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            durableActivityContextBaseMock
                .Setup(x => x.GetInput<Response.Project>())
                .Returns(fixture.Create<Response.Project>());

            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            durableOrchestrationClient
                .Setup(x => x.GetStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(fixture.Build<DurableOrchestrationStatus>()
                    .Without(x => x.Input)
                    .Without(x => x.Output)
                    .Without(x => x.History)
                    .Without(x => x.CustomStatus)
                    .Create());

            //Act
            GlobalPermissionsScanProjectActivity fun = new GlobalPermissionsScanProjectActivity(
                logAnalyticsClient.Object, fixture.Create<IVstsRestClient>(), fixture.Create<IEnvironmentConfig>(),
                ruleSets.Object);
            await fun.Run(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object);

            //Assert
            rule
                .Verify(x => x.Evaluate(It.IsAny<string>()), Times.AtLeastOnce());
            logAnalyticsClient
                .Verify(x => x.AddCustomLogJsonAsync("preventive_analysis_log", It.IsAny<Object>(), It.IsAny<string>()));
        }

        [Fact]
        public async Task RunWithNoProjectFoundFromContextShouldThrowException()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();
            var clientMock = new Mock<IVstsRestClient>();

            var rule = new Mock<IProjectRule>();
            rule
                .Setup(x => x.Evaluate(It.IsAny<string>()))
                .Returns(true);

            var rulesProvider = new Mock<IRulesProvider>();
            rulesProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new [] { rule.Object });

            //Act
            GlobalPermissionsScanProjectActivity fun = new GlobalPermissionsScanProjectActivity(logAnalyticsClient.Object, clientMock.Object, fixture.Create<IEnvironmentConfig>(), rulesProvider.Object);

            var ex = await Assert.ThrowsAsync<Exception>(async () => await fun.Run(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object));

            //Assert
            Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
        }

        [Fact]
        public async Task GeneratesReconcileUrl()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var azDoConfig = fixture.Create<EnvironmentConfig>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            
            durableActivityContextBaseMock
                .Setup(x => x.GetInput<Project>())
                .Returns(new Project {Name = "dummyproj"});
            
            var iLoggerMock = new Mock<ILogger>();
            var clientMock = new Mock<IVstsRestClient>();
            clientMock
                .Setup(x => x.Put(It.IsAny<IVstsPostRequest<GlobalPermissionsExtensionData>>(),
                    It.IsAny<GlobalPermissionsExtensionData>()));

            var rule = new Mock<IProjectRule>();
            var ruleSets = new Mock<IRuleSets>();
            ruleSets
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new[] { rule.Object });

            //Act
            GlobalPermissionsScanProjectActivity fun = new GlobalPermissionsScanProjectActivity(
                logAnalyticsClient.Object, clientMock.Object, azDoConfig, rulesProvider.Object);
            await fun.Run(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object);

            var ruleName = rule.Object.GetType().Name;

            clientMock
                .Verify(x => x.Put(It.IsAny<IVstsRestRequest<GlobalPermissionsExtensionData>>(), 
                    It.Is<GlobalPermissionsExtensionData>(d => 
                        d.Reports.Any(r => r.ReconcileUrl == $"https://{azDoConfig.FunctionAppHostname}/{azDoConfig.Organisation}/dummyproj/globalpermissions/{ruleName}"))));


        }
    }
}