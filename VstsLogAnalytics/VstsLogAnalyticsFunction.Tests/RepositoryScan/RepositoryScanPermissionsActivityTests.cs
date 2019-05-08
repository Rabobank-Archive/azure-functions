using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.GlobalPermissionsScan;
using VstsLogAnalyticsFunction.Model;
using VstsLogAnalyticsFunction.RepositoryScan;
using Xunit;
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

            var rule = mocks.Create<IRule>(MockBehavior.Loose);
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
                fixture.Create<EnvironmentConfig>(),
                new Mock<ITokenizer>().Object);
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

             var rule = new Mock<IRule>();
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
                 fixture.Create<EnvironmentConfig>(),
                 new Mock<ITokenizer>().Object);

             var ex = await Assert.ThrowsAsync<Exception>(async () => await fun.RunAsActivity(
                 durableActivityContextBaseMock.Object,
                 iLoggerMock.Object));

             //Assert
             Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
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
            
             var function = new RepositoryScanPermissionsActivity(
                 new Mock<ILogAnalyticsClient>().Object, 
                 new Mock<IVstsRestClient>().Object, 
                 new Mock<IRulesProvider>().Object,
                 fixture.Create<EnvironmentConfig>(), 
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
             var mocks = new MockRepository(MockBehavior.Loose);


             var tokenizer = new Mock<ITokenizer>();
             tokenizer
                 .Setup(x => x.Principal(It.IsAny<string>()))
                 .Returns(PrincipalWithClaims());

             var azure = mocks.Create<IVstsRestClient>();
             azure
                 .Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                 .Returns(fixture.Create<Multiple<Repository>>());

             azure
                 .Setup(x => x.Get(It.IsAny<IVstsRestRequest<ProjectProperties>>()))
                 .Returns(fixture.Create<ProjectProperties>());
             
             var request = new HttpRequestMessage();
             request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
             var function = new RepositoryScanPermissionsActivity(
                 new Mock<ILogAnalyticsClient>().Object, 
                 azure.Object, 
                 new Mock<IRulesProvider>().Object,
                 fixture.Create<EnvironmentConfig>(), 
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