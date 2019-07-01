using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Moq;
using Shouldly;
using Xunit;

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

            var function = new ProjectScanHttpStarter(tokenizer.Object);
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
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<DurableOrchestrationClientBase>();

            var function = new ProjectScanHttpStarter(tokenizer.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                mock.Object
            );

            mock.Verify(x => x .WaitForCompletionOrCreateCheckStatusResponseAsync(request,It.IsAny<string>(),It.IsAny<TimeSpan>()));
        }
        
        [Fact]
        public async Task GlobalPermissionsScopeTest()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            var mock = new Mock<DurableOrchestrationClientBase>();

            var function = new ProjectScanHttpStarter(tokenizer.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "globalpermissions",
                mock.Object
            );

            mock.Verify(x => x .StartNewAsync(nameof(GlobalPermissionsOrchestration), "TAS"));
        }

        [Fact]
        public async Task RepositoryScopeTest()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");


            var mock = new Mock<DurableOrchestrationClientBase>();
            var function = new ProjectScanHttpStarter(tokenizer.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "repository",
                mock.Object
            );

            mock.Verify(x => x.StartNewAsync(nameof(RepositoriesOrchestration), "TAS"));
        }

        [Fact]
        public async Task BuildPipelinesScopeTest()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");


            var mock = new Mock<DurableOrchestrationClientBase>();
            var function = new ProjectScanHttpStarter(tokenizer.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "buildpipelines",
                mock.Object
            );

            mock.Verify(x => x.StartNewAsync(nameof(BuildPipelinesOrchestration), "TAS"));
        }

        [Fact]
        public async Task ReleasePipelinesScopeTest()
        {
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");


            var mock = new Mock<DurableOrchestrationClientBase>();
            var function = new ProjectScanHttpStarter(tokenizer.Object);
            await function.Run(request,
                "somecompany",
                "TAS",
                "releasepipelines",
                mock.Object
            );

            mock.Verify(x => x .StartNewAsync(nameof(ReleasePipelinesOrchestration), "TAS"));
        }

        private static ClaimsPrincipal PrincipalWithClaims() =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ab84d5a2-4b8d-68df-9ad3-cc9c8884270c")
            }));
    }
}
