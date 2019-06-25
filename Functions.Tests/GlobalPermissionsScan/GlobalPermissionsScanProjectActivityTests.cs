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
using LogAnalytics.Client;
using Xunit;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Functions.GlobalPermissionsScan;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Functions.Tests.GlobalPermissionsScan
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
                .Returns(new [] { rule.Object, rule.Object });

            var durable = new Mock<DurableActivityContextBase>();
            durable
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>());

            //Act
            var fun = new GlobalPermissionsScanProjectActivity(
                fixture.Create<IVstsRestClient>(), 
                fixture.Create<EnvironmentConfig>(),
                ruleSets.Object,
                new Mock<ITokenizer>().Object);
            
            await fun.RunAsActivity(
                durable.Object,
                new Mock<ILogger>().Object);

            //Assert
            rule.Verify(x => x.Evaluate(It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunWithNoProjectFoundFromContextShouldThrowException()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            //Act
            var fun = new GlobalPermissionsScanProjectActivity(
                new Mock<IVstsRestClient>().Object, 
                fixture.Create<EnvironmentConfig>(), 
                new Mock<IRulesProvider>().Object,
                new Mock<ITokenizer>().Object);

            var ex = await Assert.ThrowsAsync<Exception>(async () => await fun.RunAsActivity(
                new Mock<DurableActivityContextBase>().Object,
                new Mock<ILogger>().Object));

            //Assert
            Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
        }

        [Fact]
        public async Task GivenGlobalPermissionsAreScanned_WhenReportsArePutInExtensionDataStorage_ThenItShouldHaveReconcileUrls()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var config = fixture.Create<EnvironmentConfig>();
            var durable = new Mock<DurableActivityContextBase>();
            
            durable
                .Setup(x => x.GetInput<Project>())
                .Returns(new Project {Name = "dummyproj"});

            var clientMock = new Mock<IVstsRestClient>();

            var rule = new Mock<IProjectRule>();
            rule
                .As<IProjectReconcile>()
                .Setup(x => x.Impact)
                .Returns(new[] { "just some action" });
            
            var rulesProvider = new Mock<IRulesProvider>();
            rulesProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new[] { rule.Object });

            //Act
            var fun = new GlobalPermissionsScanProjectActivity(
                clientMock.Object, 
                config, 
                rulesProvider.Object,
                null);
            var result = await fun.RunAsActivity(
                durable.Object,
                new Mock<ILogger>().Object);

            var ruleName = rule.Object.GetType().Name;

            // Assert
            result.Reports.ShouldContain(r => r.Reconcile != null &&
                                    r.Reconcile.Url ==
                                    $"https://{config.FunctionAppHostname}/api/reconcile/{config.Organization}/dummyproj/globalpermissions/{ruleName}" &&
                                    r.Reconcile.Impact.Any());
           result.RescanUrl.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task RunFromHttp_WithoutCredential_Unauthorized()
        {
            var fixture = new Fixture();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(new ClaimsPrincipal());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new GlobalPermissionsScanProjectActivity(
                new Mock<IVstsRestClient>().Object, 
                fixture.Create<EnvironmentConfig>(), 
                new Mock<IRulesProvider>().Object,
                tokenizer.Object);
            
            var result = await function.RunFromHttp(request , 
                "somecompany", 
                "TAS", 
                new Mock<ILogger>().Object);
                
            result.ShouldBeOfType<UnauthorizedResult>();
        }
        
        [Fact]
        public async Task RunFromHttp_WithCredential_OkResult()
        {
            var fixture = new Fixture();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new GlobalPermissionsScanProjectActivity(
                new Mock<IVstsRestClient>().Object, 
                fixture.Create<EnvironmentConfig>(), 
                new Mock<IRulesProvider>().Object,
                tokenizer.Object);
            
            var result = await function.RunFromHttp(request , 
                "somecompany", 
                "TAS", 
                new Mock<ILogger>().Object);
                
            result.ShouldBeOfType<OkResult>();
        }

        private static ClaimsPrincipal PrincipalWithClaims() => 
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ab84d5a2-4b8d-68df-9ad3-cc9c8884270c")
            }));
    }
}