using AutoFixture;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.VstsService;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

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
            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object);
            var result = await function.Run(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                new Mock<DurableOrchestrationClientBase>().Object
              );

            result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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

            var mock = new Mock<DurableOrchestrationClientBase>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(fixture.Create<Response.Project>())
                .Verifiable();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                mock.Object
            );

            mock.Verify(x => x.WaitForCompletionOrCreateCheckStatusResponseAsync(request, It.IsAny<string>(), It.IsAny<TimeSpan>()));
            client.Verify();
        }

        [Fact]
        public async Task RunFromHttp_ProjectNotFound_NotFoundResult()
        {
            var fixture = new Fixture();
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<DurableOrchestrationClientBase>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync((Response.Project)null)
                .Verifiable();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object);
            var result = await function.Run(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                mock.Object
            );

            result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            client.Verify();
        }

        [Fact]
        public async Task GlobalPermissionsScopeTest()
        {
            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<DurableOrchestrationClientBase>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(project)
                .Verifiable();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                mock.Object
            );

            mock.Verify(x => x.StartNewAsync(nameof(GlobalPermissionsOrchestration), project));
        }

        [Fact]
        public async Task RepositoryScopeTest()
        {
            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();

            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<DurableOrchestrationClientBase>();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Project>>()))
                .ReturnsAsync(project)
                .Verifiable();

            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "repository",
                mock.Object
            );

            mock.Verify(x => x.StartNewAsync(nameof(RepositoriesOrchestration), project));
        }

        [Fact]
        public async Task BuildPipelinesScopeTest()
        {
            var fixture = new Fixture();
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

            var mock = new Mock<DurableOrchestrationClientBase>();
            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "buildpipelines",
                mock.Object
            );

            mock.Verify(x => x.StartNewAsync(nameof(BuildPipelinesOrchestration), project));
        }

        [Fact]
        public async Task ReleasePipelinesScopeTest()
        {
            var fixture = new Fixture();
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

            var mock = new Mock<DurableOrchestrationClientBase>();
            var function = new ProjectScanHttpStarter(tokenizer.Object, client.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "releasepipelines",
                mock.Object
            );

            mock.Verify(x => x.StartNewAsync(nameof(ReleasePipelinesOrchestration), project));
        }

        private static ClaimsPrincipal PrincipalWithClaims() =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ab84d5a2-4b8d-68df-9ad3-cc9c8884270c")
            }));
    }
}
