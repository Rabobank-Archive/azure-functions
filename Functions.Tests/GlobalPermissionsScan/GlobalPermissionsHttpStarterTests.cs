using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Moq;
using Shouldly;
using Xunit;

namespace Functions.Tests.GlobalPermissionsScan
{
    public class GlobalPermissionsHttpStarterTests
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

            var function = new GlobalPermissionsHttpStarter(
                tokenizer.Object);

            var result = await function.RunFromHttp(request,
                "somecompany",
                "TAS",
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

            var function = new GlobalPermissionsHttpStarter(
                tokenizer.Object);

            var mock = new Mock<DurableOrchestrationClientBase>();
            await function.RunFromHttp(request,
                "somecompany",
                "TAS",
                mock.Object
            );

            mock.Verify(context=>context.WaitForCompletionOrCreateCheckStatusResponseAsync(request,It.IsAny<string>(),It.IsAny<TimeSpan>()));
        }

        private static ClaimsPrincipal PrincipalWithClaims() =>
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ab84d5a2-4b8d-68df-9ad3-cc9c8884270c")
            }));
    }
}
