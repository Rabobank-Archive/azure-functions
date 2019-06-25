using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using Functions.GlobalPermissionsScan;
using Functions.Starters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Shouldly;
using Xunit;

namespace Functions.Tests.GlobalPermissionsScan
{
    public class GlobalPermissionsHttpStarterTests
    {
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

            var function = new GlobalPermissionsHttpStarter(
                fixture.Create<EnvironmentConfig>(),
                tokenizer.Object);

            var result = await function.RunFromHttp(request,
                "somecompany",
                "TAS",
                new Mock<DurableOrchestrationClientBase>().Object
              );

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

            var function = new GlobalPermissionsHttpStarter(
                fixture.Create<EnvironmentConfig>(),
                tokenizer.Object);

            var result = await function.RunFromHttp(request,
                "somecompany",
                "TAS",
                new Mock<DurableOrchestrationClientBase>().Object
            );


            result.ShouldBeOfType<OkResult>();
        }

        private static ClaimsPrincipal PrincipalWithClaims() =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ab84d5a2-4b8d-68df-9ad3-cc9c8884270c")
            }));
    }
}
