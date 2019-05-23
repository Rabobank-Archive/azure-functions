using System.Net.Http;
using System.Security.Claims;

namespace Functions
{
    internal static class TokenizerExtension
    {
        public static string IdentifierFromClaim(this ITokenizer tokenizer, HttpRequestMessage request)
        {
            if (request.Headers.Authorization == null)
            {
                return null;
            }

            var principal = tokenizer.Principal(request.Headers.Authorization.Parameter);
            return principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        }
    }
}