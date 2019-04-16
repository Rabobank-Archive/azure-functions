using System;
using Shouldly;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class TokenizerTests
    {
        [Fact]
        public void UrlValidationToken()
        {
            var url =
                "https://azdoanalyticsdev.azurewebsites.net/api/reconcile/somecompany-test/SOx-compliant-demo/globalpermissions/NobodyCanDeleteTheTeamProject";

            var tokenizer = new Tokenizer(Guid.NewGuid());
            var token = tokenizer.Create(url);

            tokenizer.Validate(url, token).ShouldBeTrue();
        }

        [Fact]
        public void IncludeServerSecretInToken()
        {
            var url =
                "https://azdoanalyticsdev.azurewebsites.net/api/reconcile/somecompany-test/SOx-compliant-demo/globalpermissions/NobodyCanDeleteTheTeamProject";

            var tokenizer1 = new Tokenizer(Guid.NewGuid());
            var tokenizer2 = new Tokenizer(Guid.NewGuid());
            var token = tokenizer1.Create(url);

            tokenizer1.Validate(url, token).ShouldBeTrue();
            tokenizer2.Validate(url, token).ShouldBeFalse();
        }
    }
}