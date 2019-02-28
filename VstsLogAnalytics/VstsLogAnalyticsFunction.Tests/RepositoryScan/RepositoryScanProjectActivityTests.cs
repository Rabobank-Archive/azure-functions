using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using Xunit;
using Report = VstsLogAnalyticsFunction.ExtensionDataReports<SecurePipelineScan.Rules.Reports.RepositoryReport>;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanProjectActivityTests
    {
        [Fact]
        public async System.Threading.Tasks.Task GivenMultipleReposAllReposShouldBeSentToLogAnalytics()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            //Arrange
            var context = new Mock<DurableActivityContextBase>();
            context
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>);

            var scan = new Mock<IProjectScan<IEnumerable<RepositoryReport>>>(MockBehavior.Strict);
            scan
                .Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(fixture.CreateMany<RepositoryReport>());

            var azure = new Mock<IVstsRestClient>();
            azure.Setup(
                x => x.Put(
                    It.IsAny<IVstsRestRequest<Report>>(), 
                    It.IsAny<Report>()))
                .Verifiable();

            var analytics = new Mock<ILogAnalyticsClient>();
            var logger = new Mock<ILogger>();

            //Act
            RepositoryScanProjectActivity fun = new RepositoryScanProjectActivity(analytics.Object, scan.Object, azure.Object);
            await fun.Run(context.Object,
                logger.Object);

            //Assert
            analytics.Verify(x => 
              x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.AtLeastOnce());
            
            azure.Verify();
        }
    }
       
}