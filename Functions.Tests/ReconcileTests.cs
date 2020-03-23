using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Moq;
using Newtonsoft.Json;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Security;
using Shouldly;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using AzureDevOps.Compliance.Rules;
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
                .Setup(x => x.ReconcileAsync("TAS"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()));
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();
            var config = new EnvironmentConfig { Organization = "somecompany" };

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(vstsClient.Object, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new[] { rule.Object }, new IRepositoryRule[0], tokenizer.Object);
            (await function.ReconcileAsync(request,
                "somecompany",
                "TAS",
                RuleScopes.GlobalPermissions,
                rule.Object.GetType().Name)).ShouldBeOfType<OkResult>();

            rule.Verify();
        }

        [Fact]
        public async Task ExistingRepositoryRuleExecutedWhenReconcile()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var rule = new Mock<IRepositoryRule>(MockBehavior.Strict);
            rule
                .As<IReconcile>()
                .Setup(x => x.ReconcileAsync("TAS", "repository-id"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()));

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(vstsClient.Object, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new[] { rule.Object },
                tokenizer.Object);
            (await function.ReconcileAsync(request,
                "somecompany",
                "TAS",
                RuleScopes.Repositories,
                rule.Object.GetType().Name,
                "repository-id")).ShouldBeOfType<OkResult>();

            rule.Verify();
        }

        [Fact]
        public async Task CanPassPostDataToRule()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var rule = new Mock<IProjectRule>(MockBehavior.Strict);
            rule
                .As<IProjectReconcile>()
                .Setup(x => x.ReconcileAsync("TAS"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()));
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();
            var config = new EnvironmentConfig { Organization = "somecompany" };

            var json = JsonConvert.SerializeObject(new { ciIdentifier = "CI123444" });
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            request.Content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json"
                    );

            var function = new ReconcileFunction(vstsClient.Object, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new[] { rule.Object }, new IRepositoryRule[0], tokenizer.Object);
            (await function.ReconcileAsync(request,
                "somecompany",
                "TAS",
                RuleScopes.GlobalPermissions,
                rule.Object.GetType().Name)).ShouldBeOfType<OkResult>();

            rule.Verify();
        }

        [Fact]
        public async Task RuleNotFound()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(vstsClient.Object, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0],
                tokenizer.Object);
            var result = (await function.ReconcileAsync(request,
                "somecompany",
                "TAS",
                RuleScopes.GlobalPermissions,
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

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.PermissionsProjectId>>()))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");


            var function = new ReconcileFunction(vstsClient.Object,
                new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0], tokenizer.Object);
            var result = (await function.ReconcileAsync(request,
                "somecompany",
                "TAS",
                "non-existing-scope",
                "some-non-existing-rule")).ShouldBeOfType<NotFoundObjectResult>();

            result.Value.ShouldBe("non-existing-scope");
        }

        [Fact]
        public async Task CanCheckPermissionsForUserWithUnknownVsIInTokenAndValidUserId()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult<Response.PermissionsProjectId>(null))
                .Verifiable();

            vstsClient
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ef2e3683-8fb5-439d-9dc9-53af732e6387"))))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            request.RequestUri = new System.Uri("https://dev.azure.com/reconcile/somecompany/TAS/haspermissions?userId=ef2e3683-8fb5-439d-9dc9-53af732e6387");

            var function = new ReconcileFunction(vstsClient.Object,
                new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0], tokenizer.Object);
            (await function
                    .HasPermissionAsync(request, "somecompany", "TAS"))
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(true);
            vstsClient.Verify();
        }

        [Fact]
        public async Task CanCheckPermissionsForUserWithUnknownVsIInTokenAndInvalidUserId()
        {
            var fixture = new Fixture();
            ManageProjectPropertiesPermission(fixture);

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult<Response.PermissionsProjectId>(null))
                .Verifiable();

            vstsClient
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ef2e3683-8fb5-439d-9dc9-53af732e6387"))))
                .Returns(Task.FromResult<Response.PermissionsProjectId>(null))
                .Verifiable();

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            request.RequestUri =
                new System.Uri(
                    "https://dev.azure.com/reconcile/somecompany/TAS/haspermissions?userId=ef2e3683-8fb5-439d-9dc9-53af732e6387");

            var function = new ReconcileFunction(vstsClient.Object,
                new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0], tokenizer.Object);
            (await function
                    .HasPermissionAsync(request, "somecompany", "TAS"))
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(false);
            vstsClient.Verify();
        }

        [Fact]
        public async Task UnauthorizedWithoutHeaderWhenReconcile()
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = null;

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var function = new ReconcileFunction(null, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0],
                new Mock<ITokenizer>().Object);
            (await function.ReconcileAsync(request,
                "somecompany",
                "TAS",
                RuleScopes.GlobalPermissions,
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

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var function = new ReconcileFunction(null, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0],
                tokenizer.Object);
            (await function.ReconcileAsync(request,
                "somecompany",
                "TAS",
                RuleScopes.GlobalPermissions,
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

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(vstsClient.Object,
                new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0], tokenizer.Object);
            (await function.ReconcileAsync(request,
                    "somecompany",
                    "TAS",
                    RuleScopes.GlobalPermissions,
                    "some-non-existing-rule"))
                .ShouldBeOfType<UnauthorizedResult>();

            vstsClient.Verify();
        }

        [Fact]
        public async Task UnauthorizedWithoutHeaderWhenHasPermission()
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = null;

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var function = new ReconcileFunction(null, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0],
                new Mock<ITokenizer>().Object);
            (await function
                    .HasPermissionAsync(request, "somecompany", "TAS"))
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

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var function = new ReconcileFunction(null, new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0],
                tokenizer.Object);
            (await function
                    .HasPermissionAsync(request, "somecompany", "TAS"))
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

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(vstsClient.Object,
                new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0], tokenizer.Object);
            (await function
                    .HasPermissionAsync(request, "somecompany", "TAS"))
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(false);
            vstsClient.Verify();
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

            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.GetAsync(It.Is<IVstsRequest<Response.PermissionsProjectId>>(req =>
                    req.QueryParams.Values.Contains("ab84d5a2-4b8d-68df-9ad3-cc9c8884270c"))))
                .Returns(Task.FromResult(fixture.Create<Response.PermissionsProjectId>()))
                .Verifiable();

            var config = new EnvironmentConfig { Organization = "somecompany" };
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var function = new ReconcileFunction(vstsClient.Object,
                new IBuildPipelineRule[0], new IReleasePipelineRule[0], new IProjectRule[0], new IRepositoryRule[0], tokenizer.Object);
            (await function
                    .HasPermissionAsync(request, "somecompany", "TAS"))
                .ShouldBeOfType<OkObjectResult>()
                .Value
                .ShouldBe(true);
            vstsClient.Verify();
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