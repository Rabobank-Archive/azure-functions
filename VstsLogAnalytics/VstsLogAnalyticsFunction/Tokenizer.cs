using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VstsLogAnalyticsFunction
{
    public interface ITokenizer
    {
        ClaimsPrincipal Principal(string token);
        string Token(params Claim[] claims);
    }

    public class Tokenizer : ITokenizer
    {
        private readonly byte[] _key;

        public Tokenizer(string secret)
        {
            if (secret == null) throw new ArgumentNullException(nameof(secret));
            _key = Encoding.ASCII.GetBytes(secret);
        }

        public ClaimsPrincipal Principal(string token)
        {
            var parameters = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKeys = new[] {new SymmetricSecurityKey(_key)}
            };
            
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, parameters, out _);
        }

        public string Token(params Claim[] claims)
        {
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }
    }
}