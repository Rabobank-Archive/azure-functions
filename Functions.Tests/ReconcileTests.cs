using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Shouldly;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests
{
    public class ReconcileTests
    {
        [Fact]
        public async Task ExistingRuleExecutedWhenReconcile()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var rule = new Mock<IProjectRule>(MockBehavior.Strict);
            rule
                .As<IProjectReconcile>()
                .Setup(x => x.Reconcile("TAS"))
                .Returns(Task.CompletedTask)
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
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()));

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(client.Object, ruleProvider.Object, tokenizer.Object);
            (await function.Reconcile(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                rule.Object.GetType().Name)).ShouldBeOfType<OkResult>();

            rule.Verify();
        }

        [Fact]
        public async Task ExistingRepositoryRuleExecutedWhenReconcile()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var rule = new Mock<IRule>(MockBehavior.Strict);
            rule
                .As<IReconcile>()
                .Setup(x => x.Reconcile("TAS", "repository-id"))
                .Returns(Task.CompletedTask)
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
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()));

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(client.Object, ruleProvider.Object, tokenizer.Object);
            (await function.Reconcile(request,
                "somecompany",
                "TAS",
                "repository",
                rule.Object.GetType().Name,
                "repository-id")).ShouldBeOfType<OkResult>();

            rule.Verify();
        }

        [Fact]
        public async Task RuleNotFound()
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
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(client.Object, ruleProvider.Object, tokenizer.Object);
            var result = (await function.Reconcile(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                "some-non-existing-rule")).ShouldBeOfType<NotFoundObjectResult>();

            result
                .Value
                .ToString()
                .ShouldContain("Rule not found");
        }

        [Fact]
        public async Task ScopeNotFound()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);


            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(client.Object, new Mock<IRulesProvider>().Object, tokenizer.Object);
            var result = (await function.Reconcile(request,
                "somecompany",
                "TAS",
                "non-existing-scope",
                "some-non-existing-rule")).ShouldBeOfType<NotFoundObjectResult>();

            result.Value.ShouldBe("non-existing-scope");
        }

        [Fact]
        public async Task UnauthorizedWithoutHeaderWhenReconcile()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            var request = new HttpRequestMessage();
            request.Headers.Authorization = null;

            var function = new ReconcileFunction(null, ruleProvider.Object, new Mock<ITokenizer>().Object);
            (await function.Reconcile(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                "some-non-existing-rule")).ShouldBeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task UnauthorizedWithoutNameClaimWhenReconcile()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(new ClaimsPrincipal());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(null, new Mock<IRulesProvider>().Object, tokenizer.Object);
            (await function.Reconcile(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                "some-non-existing-rule")).ShouldBeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task UnauthorizedWithoutPermissionWhenReconcile()
        {
            var fixture = new Fixture();
            fixture.Customize<Response.Permission>(ctx =>
                ctx.With(x => x.DisplayName, "Manage project properties"));

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(client.Object, new Mock<IRulesProvider>().Object, tokenizer.Object);
            (await function.Reconcile(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                "some-non-existing-rule"))
                .ShouldBeOfType<UnauthorizedResult>();

            client.Verify();
        }

        [Fact]
        public async Task UnauthorizedWithoutHeaderWhenHasPermission()
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = null;

            var function = new ReconcileFunction(null, new Mock<IRulesProvider>().Object, new Mock<ITokenizer>().Object);
            (await function
                .HasPermission(request, "somecompany", "TAS"))
                .ShouldBeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task UnauthorizedWithoutNameClaimWhenHasPermission()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(new ClaimsPrincipal());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(null, new Mock<IRulesProvider>().Object, tokenizer.Object);
            (await function
                .HasPermission(request, "somecompany", "TAS"))
                .ShouldBeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task WithoutPermission()
        {
            var fixture = new Fixture();
            fixture.Customize<Response.Permission>(ctx =>
                ctx.With(x => x.DisplayName, "Manage project properties"));

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(client.Object, new Mock<IRulesProvider>().Object, tokenizer.Object);
            (await function
                .HasPermission(request, "somecompany", "TAS"))
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(false);
            client.Verify();
        }

        [Fact]
        public async Task WithPermission()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(client.Object, new Mock<IRulesProvider>().Object, tokenizer.Object);
            (await function
                .HasPermission(request, "somecompany", "TAS"))
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(true);
            client.Verify();
        }

        private static void ManageProjectPropertiesPermission(IFixture fixture)
        {
            fixture.Customize<Response.Permission>(ctx => ctx
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