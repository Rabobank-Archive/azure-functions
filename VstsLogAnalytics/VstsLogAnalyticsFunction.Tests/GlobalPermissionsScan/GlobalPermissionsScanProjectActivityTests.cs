using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using VstsLogAnalytics.Client;
using Xunit;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
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
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>());

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
            var fun = new GlobalPermissionsScanProjectActivity(
                logAnalyticsClient.Object, 
                fixture.Create<IVstsRestClient>(), 
                fixture.Create<EnvironmentConfig>(),
                ruleSets.Object,
                new Mock<ITokenizer>().Object);
            await fun.RunAsActivity(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object);

            //Assert
            rule.Verify(x => x.Evaluate(It.IsAny<string>()), Times.AtLeastOnce());
            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync("preventive_analysis_log", It.IsAny<Object>(), It.IsAny<string>()));
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
            var fun = new GlobalPermissionsScanProjectActivity(
                logAnalyticsClient.Object, 
                clientMock.Object, 
                fixture.Create<IEnvironmentConfig>(), 
                rulesProvider.Object,
                new Mock<ITokenizer>().Object);

            var ex = await Assert.ThrowsAsync<Exception>(async () => await fun.RunAsActivity(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object));

            //Assert
            Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
        }

        [Fact]
        public async Task GivenGlobalPermissionsAreScanned_WhenReportsArePutInExtensionDataStorage_ThenItShouldHaveReconcileUrlsAndToken()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var azDoConfig = fixture.Create<EnvironmentConfig>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            
            durableActivityContextBaseMock
                .Setup(x => x.GetInput<Project>())
                .Returns(new Project {Name = "dummyproj"});

            var clientMock = new Mock<IVstsRestClient>();

            var rule = new Mock<IProjectRule>();
            var rulesProvider = new Mock<IRulesProvider>();
            rulesProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new[] { rule.Object });

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Token(It.IsAny<Claim[]>()))
                .Returns("token");

            //Act
            var fun = new GlobalPermissionsScanProjectActivity(
                new Mock<ILogAnalyticsClient>().Object, 
                clientMock.Object, 
                azDoConfig, 
                rulesProvider.Object,
                tokenizer.Object);
            await fun.RunAsActivity(
                durableActivityContextBaseMock.Object,
                new Mock<ILogger>().Object);

            var ruleName = rule.Object.GetType().Name;

            // Assert
            clientMock
                .Verify(x => x.Put(It.IsAny<IVstsRestRequest<GlobalPermissionsExtensionData>>(), 
                    It.Is<GlobalPermissionsExtensionData>(d => 
                        d.Reports.Any(r => r.ReconcileUrl == $"https://{azDoConfig.FunctionAppHostname}/api/reconcile/{azDoConfig.Organisation}/dummyproj/globalpermissions/{ruleName}") && 
                        d.Token == "token")));
        }
        
        [Fact]
        public async Task RunFromHttp_RejectsCallIfScopeDoesntMatch()
        {
            var fixture = new Fixture();
            var clientMock = new Mock<IVstsRestClient>();

            var rulesProvider = new Mock<IRulesProvider>();
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims("CCC"));

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new GlobalPermissionsScanProjectActivity(
                new Mock<ILogAnalyticsClient>().Object, 
                clientMock.Object, 
                fixture.Create<EnvironmentConfig>(), 
                rulesProvider.Object,
                tokenizer.Object);
            
            var result = await function.RunFromHttp(request , 
                "somecompany", 
                "TAS", 
                new Mock<ILogger>().Object);
                
            result.ShouldBeOfType<UnauthorizedResult>();
        }

        private static ClaimsPrincipal PrincipalWithClaims(string project = "TAS") => 
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("organization", "somecompany"),
                new Claim("project", project)
            }));
    }
}