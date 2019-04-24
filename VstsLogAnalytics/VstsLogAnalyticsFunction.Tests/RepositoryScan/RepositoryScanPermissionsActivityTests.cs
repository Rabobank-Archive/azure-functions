using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using System.Threading.Tasks;
using VstsLogAnalyticsFunction.GlobalPermissionsScan;
using VstsLogAnalyticsFunction.RepositoryScan;
using Xunit;
using Report = VstsLogAnalyticsFunction.ExtensionDataReports<SecurePipelineScan.Rules.Reports.RepositoryReport>;
using Task = System.Threading.Tasks.Task;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanPermissionsActivityTests
    {
        [Fact]
         public async Task RunShouldCallIProjectRuleEvaluate()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var iLoggerMock = new Mock<ILogger>();

            var rule = new Mock<IRepositoryRule>();
            rule
                .Setup(x => x.Evaluate(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            var ruleSets = new Mock<IRulesProvider>();
            ruleSets
                .Setup(x => x.RepositoryRules(It.IsAny<IVstsRestClient>()))
                .Returns(new [] { rule.Object });


            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            durableActivityContextBaseMock
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>());
            
            
            var azure = new Mock<IVstsRestClient>(MockBehavior.Strict);
            azure
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(fixture.Create<Multiple<Repository>>());

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
            var fun = new RepositoryScanPermissionsActivity(
                logAnalyticsClient.Object, 
                azure.Object, 
                fixture.Create<EnvironmentConfig>(),
                ruleSets.Object,
                new Mock<ITokenizer>().Object);
            await fun.Run(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object);

            //Assert
            rule.Verify(x => x.Evaluate(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync("preventive_analysis_log", It.IsAny<Object>(), It.IsAny<string>()));
        }
    }
       
}

//        [Fact]
//        public async Task shouldGetTheRepositories()
//        {
//            var fixture = new Fixture();
//            fixture.Customize(new AutoMoqCustomization());
//
//            //Arrange
//            var context = new Mock<DurableActivityContextBase>();
//            context
//                .Setup(x => x.GetInput<Project>())
//                .Returns(fixture.Create<Project>);
//            
//            var azure = new Mock<IVstsRestClient>();
//            azure
//                .Setup(x => x.Get(It.IsAny<VstsRestRequest<Multiple<Repository>>>()))
//                .Returns(fixture.Create<Multiple<Repository>>);
//        }