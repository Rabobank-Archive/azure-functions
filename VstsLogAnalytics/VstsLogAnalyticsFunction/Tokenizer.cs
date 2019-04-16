using System;
using Microsoft.AspNet.Identity;

namespace VstsLogAnalyticsFunction
{
    public interface ITokenizer
    {
        bool Validate(string url, string token);
        string Create(string url);
    }

    public class Tokenizer : ITokenizer
    {
        private readonly Guid _secret;
        private readonly PasswordHasher _hasher = new PasswordHasher();

        public Tokenizer(Guid secret) => 
            _secret = secret;

        public bool Validate(string url, string token) => 
            _hasher.VerifyHashedPassword(token, IncludeSecret(url)) == PasswordVerificationResult.Success;

        public string Create(string url) => 
            _hasher.HashPassword(IncludeSecret(url));

        private string IncludeSecret(string url) => url + _secret;
    }
}