using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.Model;
using VstsLogAnalyticsFunction.RepositoryScan;
using Xunit;
using Report = VstsLogAnalyticsFunction.ExtensionDataReports<SecurePipelineScan.Rules.Reports.RepositoryReport>;
using Task = System.Threading.Tasks.Task;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanPermissionsActivityTests
    {
        [Fact]
        public async Task RunShouldCallIProjectRuleEvaluateAndStoreToLogAnalytics()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var mocks = new MockRepository(MockBehavior.Strict);
            var analytics = mocks.Create<ILogAnalyticsClient>();
            analytics
                .Setup(x => x.AddCustomLogJsonAsync("preventive_analysis_log", It.IsAny<object>(), "evaluatedDate"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var rule = mocks.Create<IRepositoryRule>(MockBehavior.Loose);
            rule
                .Setup(x => x.Evaluate(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true)
                .Verifiable();

            var ruleSets = mocks.Create<IRulesProvider>();
            ruleSets
                .Setup(x => x.RepositoryRules(It.IsAny<IVstsRestClient>()))
                .Returns(new [] { rule.Object });


            var durable = mocks.Create<DurableActivityContextBase>();
            durable
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>());
            
            
            var azure = mocks.Create<IVstsRestClient>();
            azure
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(fixture.Create<Multiple<Repository>>());
            azure
                .Setup(x => x.Put(
                    It.IsAny<ExtmgmtRequest<RepositoriesExtensionData>>(),
                    It.IsAny<RepositoriesExtensionData>()))
                .Returns((object req, RepositoriesExtensionData data) => data)
                .Verifiable();

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
                analytics.Object,
                azure.Object,
                ruleSets.Object,
                fixture.Create<EnvironmentConfig>());
            await fun.RunAsActivity(
                durable.Object,
                new Mock<ILogger>().Object);

            //Assert
            mocks.Verify();
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

             var rule = new Mock<IRepositoryRule>();
             rule
                 .Setup(x => x.Evaluate(It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(true);

             var rulesProvider = new Mock<IRulesProvider>();
             rulesProvider
                 .Setup(x => x.RepositoryRules(It.IsAny<IVstsRestClient>()))
                 .Returns(new [] { rule.Object });
           
             //Act
             var fun = new RepositoryScanPermissionsActivity(
                 logAnalyticsClient.Object, 
                 clientMock.Object, 
                 rulesProvider.Object,
                 fixture.Create<EnvironmentConfig>());

             var ex = await Assert.ThrowsAsync<Exception>(async () => await fun.RunAsActivity(
                 durableActivityContextBaseMock.Object,
                 iLoggerMock.Object));

             //Assert
             Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
         }
    }
       
}