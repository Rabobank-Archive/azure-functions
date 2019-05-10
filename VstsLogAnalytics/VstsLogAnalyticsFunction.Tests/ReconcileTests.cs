using System.Linq;
using System.Net;
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
        public void ExistingRuleExecutedWhenReconcile()
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
                .Returns(fixture.Create<PermissionsProjectId>());
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(client.Object, ruleProvider.Object, tokenizer.Object);
            function.Reconcile(request, 
                "somecompany", 
                "TAS", 
                "globalpermissions",
                rule.Object.GetType().Name).ShouldBeOfType<OkResult>();
            
            rule.Verify();
        }
        
        [Fact]
        public void ExistingRepositoryRuleExecutedWhenReconcile()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var rule = new Mock<IRule>(MockBehavior.Strict);
            rule
                .As<IReconcile>()
                .Setup(x => x.Reconcile("TAS", "repository-id"))
                .Verifiable();
                
            var ruleProvider = new Mock<IRulesProvider>();
            ruleProvider
                .Setup(x => x.RepositoryRules(It.IsAny<IVstsRestClient>()))
                .Returns(new[] { rule.Object });
            
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());
            
            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<PermissionsProjectId>>()))
                .Returns(fixture.Create<PermissionsProjectId>());
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(client.Object, ruleProvider.Object, tokenizer.Object);
            function.Reconcile(request, 
                "somecompany", 
                "TAS", 
                "repository",
                rule.Object.GetType().Name,
                "repository-id").ShouldBeOfType<OkResult>();
            
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
            var result = function.Reconcile(request, 
                "somecompany", 
                "TAS", 
                "globalpermissions",
                "some-non-existing-rule").ShouldBeOfType<NotFoundObjectResult>();

            result
                .Value
                .ToString()
                .ShouldContain("Rule not found");
        }
        
        [Fact]
        public void ScopeNotFound()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);


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
            
            var function = new ReconcileFunction(client.Object, new Mock<IRulesProvider>().Object, tokenizer.Object);
            var result = function.Reconcile(request, 
                "somecompany", 
                "TAS", 
                "non-existing-scope",
                "some-non-existing-rule").ShouldBeOfType<NotFoundObjectResult>();

            result.Value.ShouldBe("non-existing-scope");
        }

        [Fact]
        public void UnauthorizedWithoutHeaderWhenReconcile()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            var request = new HttpRequestMessage();
            request.Headers.Authorization = null;
            
            var function = new ReconcileFunction(null, ruleProvider.Object, new Mock<ITokenizer>().Object);
            function.Reconcile(request , 
                "somecompany", 
                "TAS", 
                "globalpermissions",
                "some-non-existing-rule").ShouldBeOfType<UnauthorizedResult>();
        }
            
        [Fact]
        public void UnauthorizedWithoutNameClaimWhenReconcile()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(new ClaimsPrincipal());
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, new Mock<IRulesProvider>().Object, tokenizer.Object);
            function.Reconcile(request , 
                "somecompany", 
                "TAS", 
                "globalpermissions",
                "some-non-existing-rule").ShouldBeOfType<UnauthorizedResult>();
        }
        
        [Fact]
        public void UnauthorizedWithoutPermissionWhenReconcile()
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
            function.Reconcile(request , 
                "somecompany", 
                "TAS", 
                "globalpermissions",
                "some-non-existing-rule")
                .ShouldBeOfType<UnauthorizedResult>();
            
            client.Verify();
        }
        
        [Fact]
        public void UnauthorizedWithoutHeaderWhenHasPermission()
        {           
            var request = new HttpRequestMessage();
            request.Headers.Authorization = null;
            
            var function = new ReconcileFunction(null, new Mock<IRulesProvider>().Object, new Mock<ITokenizer>().Object);
            function
                .HasPermission(request, "somecompany","TAS")
                .ShouldBeOfType<UnauthorizedResult>();
        }
            
        [Fact]
        public void UnauthorizedWithoutNameClaimWhenHasPermission()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(new ClaimsPrincipal());
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, new Mock<IRulesProvider>().Object, tokenizer.Object);
            function
                .HasPermission(request,"somecompany", "TAS")
                .ShouldBeOfType<UnauthorizedResult>();
        }
        
        [Fact]
        public void WithoutPermission()
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
            function
                .HasPermission(request,"somecompany","TAS")
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(false);
            client.Verify();
        }

        [Fact]
        public void WithPermission()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);
                        
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
            function
                .HasPermission(request,"somecompany","TAS")
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(true);
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