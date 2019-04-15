using System.Linq;
using System.Net.Http;
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
            
            var function = new ReconcileFunction(null, ruleProvider.Object);
            function.Run(new Mock<HttpRequestMessage>().Object, 
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
            
            var function = new ReconcileFunction(null, ruleProvider.Object);
            var result = function.Run(new Mock<HttpRequestMessage>().Object, 
                "somecompany", 
                "TAS", 
                "some-non-existing-rule").ShouldBeOfType<NotFoundObjectResult>();

            result
                .Value
                .ToString()
                .ShouldContain("Rule not found");
        }
    }
}