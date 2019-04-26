using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class ReconcileTests
    {
        [Fact]
        public void ExistingRuleExecuted()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var rule = new Mock<IProjectRule>(MockBehavior.Strict);
            rule
                .As<IProjectReconcile>()
                .Setup(x => x.Reconcile("TAS"))
                .Verifiable();
                
            var ruleProvider = new Mock<IRulesProvider>();
            ruleProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new[] { rule.Object });
            
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());
            
            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<PermissionsProjectId>>()))
                .Returns(fixture.Create<PermissionsProjectId>())
                .Verifiable();
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(client.Object, ruleProvider.Object, tokenizer.Object);
            function.Run(request, 
                "somecompany", 
                "TAS", 
                rule.Object.GetType().Name).ShouldBeOfType<OkResult>();
            
            rule.Verify();
        }

        [Fact]
        public void RuleNotFound()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var ruleProvider = new Mock<IRulesProvider>();
            ruleProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(Enumerable.Empty<IProjectRule>());
            
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());
            
            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<PermissionsProjectId>>()))
                .Returns(fixture.Create<PermissionsProjectId>())
                .Verifiable();
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(client.Object, ruleProvider.Object, tokenizer.Object);
            var result = function.Run(request, 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<NotFoundObjectResult>();

            result
                .Value
                .ToString()
                .ShouldContain("Rule not found");
        }

        [Fact]
        public void UnauthorizedWithoutHeader()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            var request = new HttpRequestMessage();
            request.Headers.Authorization = null;
            
            var function = new ReconcileFunction(null, ruleProvider.Object, new Mock<ITokenizer>().Object);
            function.Run(request , 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<UnauthorizedResult>();
        }
            
        [Fact]
        public void UnauthorizedWithoutNameClaim()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(new ClaimsPrincipal());
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, new Mock<IRulesProvider>().Object, tokenizer.Object);
            function.Run(request , 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<UnauthorizedResult>();
        }
        
        [Fact]
        public void UnauthorizedWithoutPermission()
        {
            var fixture = new Fixture();
            fixture.Customize<Permission>(ctx =>
                ctx.With(x => x.DisplayName, "Manage project properties"));
                        
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.Is<IVstsRestRequest<PermissionsProjectId>>(req => req.Uri.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(fixture.Create<PermissionsProjectId>())
                .Verifiable();
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(client.Object, new Mock<IRulesProvider>().Object, tokenizer.Object);
            function.Run(request , 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule")
                .ShouldBeOfType<UnauthorizedResult>();
            
            client.Verify();
        }
        
        private static void ManageProjectPropertiesPermission(IFixture fixture)
        {
            fixture.Customize<Permission>(ctx => ctx
                .With(x => x.DisplayName, "Manage project properties")
                .With(x => x.PermissionId, 3));
        }

        private static ClaimsPrincipal PrincipalWithClaims() => 
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ab84d5a2-4b8d-68df-9ad3-cc9c8884270c")
            }));
    }
}