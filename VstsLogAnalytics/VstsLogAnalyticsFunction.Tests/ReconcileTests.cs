using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Shouldly;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class ReconcileTests
    {
        [Fact]
        public void ExistingRuleExecuted()
        {
            var rule = new Mock<IProjectRule>(MockBehavior.Strict);
            rule
                .As<IProjectReconcile>()
                .Setup(x => x.Reconcile("TAS"))
                .Verifiable();
                
                
            var ruleProvider = new Mock<IRulesProvider>();
            ruleProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(new[] { rule.Object });
            
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, ruleProvider.Object, tokenizer.Object);
            function.Run(request, 
                "somecompany", 
                "TAS", 
                rule.Object.GetType().Name);
            
            rule.Verify();
        }
        
        [Fact]
        public void RuleNotFound()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            ruleProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(Enumerable.Empty<IProjectRule>());
            
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());
            
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, ruleProvider.Object, tokenizer.Object);
            var result = function.Run(request, 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<NotFoundObjectResult>();

            result
                .Value
                .ToString()
                .ShouldContain("Rule not found");
        }

        [Fact]
        public void RejectsCallIfTokenDoesntMatch()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(() => null);

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, ruleProvider.Object, tokenizer.Object);
            function.Run(request , 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<UnauthorizedResult>();
        }
        
        [Fact]
        public void RejectsCallIfScopeDoesntMatch()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims("CCC"));

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, ruleProvider.Object, tokenizer.Object);
            function.Run(request , 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<UnauthorizedResult>();
        }
        
        [Fact]
        public void RejectsCallIfOrganizationDoesntMatch()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            var tokenizer = new Mock<ITokenizer>();
            tokenizer
                .Setup(x => x.Principal(It.IsAny<string>()))
                .Returns(PrincipalWithClaims());

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");
            
            var function = new ReconcileFunction(null, ruleProvider.Object, tokenizer.Object);
            function.Run(request , 
                "some-other-organization", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<UnauthorizedResult>();
        }

        private static ClaimsPrincipal PrincipalWithClaims(string project = "TAS") => 
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("organization", "somecompany"),
                new Claim("project", project)
            }));
    }
}