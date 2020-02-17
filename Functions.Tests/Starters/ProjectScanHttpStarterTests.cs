using AutoFixture;
using Functions.Orchestrators;
using Functions.Starters;
using Moq;
using SecurePipelineScan.VstsService;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Security;
using Functions.Helpers;
using AutoFixture.AutoMoq;

namespace Functions.Tests.Starters
{
    public class ProjectScanHttpStarterTests
    {
        [Fact]
        public async Task RunFromHttp_WithoutCredential_Unauthorized()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(new ClaimsPrincipal());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object, fixture.Create<PoliciesResolver>());
            var result = await function.RunAsync(request, "somecompany", "TAS", RuleScopes.GlobalPermissions,
                new Mock<IDurableOrchestrationClient>().Object);

            result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            result.Dispose();
        }

        [Fact]
        public async Task RunFromHttp_WithCredential_OkResult()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<IDurableOrchestrationClient>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(fixture.Create<Response.Project>())
                .Verifiable();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object, fixture.Create<PoliciesResolver>());
            var result = await function.RunAsync(request, "somecompany", "TAS", RuleScopes.GlobalPermissions,
                mock.Object);

            mock.Verify(x => x.WaitForCompletionOrCreateCheckStatusResponseAsync(request, It.IsAny<string>(),
                It.IsAny<TimeSpan>(), TimeSpan.FromSeconds(1)));
            client.Verify();
            result?.Dispose();
        }

        [Fact]
        public async Task RunFromHttp_ProjectNotFound_NotFoundResult()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<IDurableOrchestrationClient>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync((Response.Project)null)
                .Verifiable();

            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object, fixture.Create<PoliciesResolver>());
            var result = await function.RunAsync(request, "somecompany", "TAS", RuleScopes.GlobalPermissions,
                mock.Object);

            result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            client.Verify();
            result.Dispose();
        }

        [Fact]
        public async Task GlobalPermissionsScopeTest()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var project = fixture.Create<Response.Project>();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<IDurableOrchestrationClient>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(project)
                .Verifiable();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object, fixture.Create<PoliciesResolver>());
            await function.RunAsync(request, "somecompany", "TAS", RuleScopes.GlobalPermissions, mock.Object);

            mock.Verify(x => x.StartNewAsync<object>(nameof(ProjectScanOrchestrator), string.Empty,
                It.Is<(Response.Project, string, DateTime)>(t => t.Item1 == project && t.Item2 == RuleScopes.GlobalPermissions)));
        }

        [Fact]
        public async Task RepositoryScopeTest()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var project = fixture.Create<Response.Project>();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<IDurableOrchestrationClient>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(project)
                .Verifiable();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object, fixture.Create<PoliciesResolver>());
            await function.RunAsync(request, "somecompany", "TAS", RuleScopes.Repositories, mock.Object);

            mock.Verify(x => x.StartNewAsync<object>(nameof(ProjectScanOrchestrator), string.Empty,
                It.Is<(Response.Project, string, DateTime)>(t => t.Item1 == project && t.Item2 == RuleScopes.Repositories)));
        }

        [Fact]
        public async Task BuildPipelinesScopeTest()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var project = fixture.Create<Response.Project>();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(project)
                .Verifiable();

            var mock = new Mock<IDurableOrchestrationClient>();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object, fixture.Create<PoliciesResolver>());
            await function.RunAsync(request, "somecompany", "TAS", RuleScopes.BuildPipelines, mock.Object);

            mock.Verify(x => x.StartNewAsync<object>(nameof(ProjectScanOrchestrator), string.Empty,
                It.Is<(Response.Project, string, DateTime)>(t => t.Item1 == project && t.Item2 == RuleScopes.BuildPipelines)));
        }

        [Fact]
        public async Task ReleasePipelinesScopeTest()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var project = fixture.Create<Response.Project>();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(project)
                .Verifiable();

            var mock = new Mock<IDurableOrchestrationClient>();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object, fixture.Create<PoliciesResolver>());
            await function.RunAsync(request, "somecompany", "TAS", RuleScopes.ReleasePipelines, mock.Object);

            mock.Verify(x => x.StartNewAsync<object>(nameof(ProjectScanOrchestrator), string.Empty,
                It.Is<(Response.Project, string, DateTime)>(t => t.Item1 == project && t.Item2 == RuleScopes.ReleasePipelines)));
        }

        private static ClaimsPrincipal PrincipalWithClaims() =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ab84d5a2-4b8d-68df-9ad3-cc9c8884270c")
            }));
    }
}