using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RestSharp;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Client;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction.Tests
{
    public class SecurityScanFunctionTest
    {
        [Fact]
        public async Task ProductionOwnerResponseShouldBeSentToLogAnalytics()
        {
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            MockProjects(client);

            MockApplicationGroups(client);

            MockSecurityNameSpaces(client);

            MockRepositoryAndPermissions(client, fixture);

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, client.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task LogNameInAddCustomLogJsonAsyncShouldBeSecurityScanReport()
        {
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            MockProjects(client);

            MockApplicationGroups(client);

            MockSecurityNameSpaces(client);

            MockRepositoryAndPermissions(client, fixture);

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, client.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<object>(), It.IsAny<string>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunShouldGetAllAzDoProjects()
        {
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            MockProjects(client);

            MockApplicationGroups(client);

            MockSecurityNameSpaces(client);

            MockRepositoryAndPermissions(client, fixture);

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, client.Object, new Mock<ILogger>().Object);

            client.Verify(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<Response.Project>>>()),
                Times.AtLeastOnce());
        }
        
        private static void MockRepositoryAndPermissions(Mock<IVstsRestClient> client, Fixture fixture)
        {
            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.ProjectProperties>>()))
                .Returns(fixture.Create<Response.ProjectProperties>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Repository>>>()))
                .Returns(fixture.Create<Response.Multiple<SecurePipelineScan.VstsService.Response.Repository>>());
            
            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.PermissionsSetId>>()))
                .Returns(fixture.Create<Response.PermissionsSetId>());

        }

        private static void MockSecurityNameSpaces(Mock<IVstsRestClient> client)
        {
            var names = new Response.Multiple<Response.SecurityNamespace>(new Response.SecurityNamespace
            {
                DisplayName = "Git Repositories",
                NamespaceId = "123456"
            });

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.SecurityNamespace>>>()))
                .Returns(names);
        }

        private static void MockApplicationGroups(Mock<IVstsRestClient> client)
        {
            var applicationGroup1 = new Response.ApplicationGroup {DisplayName = "[TAS]\\Project Administrators", TeamFoundationId = "1234",};
            var applicationGroup2 = new Response.ApplicationGroup {DisplayName = "[TAS]\\Rabobank Project Administrators"};
            var applicationGroups = new Response.ApplicationGroups {Identities = new[] {applicationGroup1, applicationGroup2}};

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>())).Returns(applicationGroups);
        }
        
        private static void MockProjects(Mock<IVstsRestClient> client)
        {
            var allProjects = new Response.Multiple<Response.Project>(new Response.Project
            {
                Id = "1",
                Name = "TAS"
            });


            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(allProjects);
        }
    }
}