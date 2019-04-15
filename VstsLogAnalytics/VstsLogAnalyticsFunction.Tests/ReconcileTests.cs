using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Shouldly;
using VstsLogAnalyticsFunction.GlobalPermissionsScan;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class ReconcileTests
    {
        [Fact]
        public async Task ExistingRuleExecutedAndGlobalPermissionFunctionCalled()
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
            
            var context = new Mock<DurableOrchestrationContextBase>();
            context
                .Setup(x => x.CallActivityAsync(nameof(GlobalPermissionsScanProjectActivity), It.IsAny<object>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var function = new ReconcileFunction(null, ruleProvider.Object);            
            await function.Run(new Mock<HttpRequestMessage>().Object, 
                context.Object,
                "somecompany", 
                "TAS", 
                rule.Object.GetType().Name);
            
            rule.Verify();
            context.Verify();
        }
        
        [Fact]
        public async Task RuleNotFound()
        {
            var ruleProvider = new Mock<IRulesProvider>();
            ruleProvider
                .Setup(x => x.GlobalPermissions(It.IsAny<IVstsRestClient>()))
                .Returns(Enumerable.Empty<IProjectRule>());
            
            var function = new ReconcileFunction(null, ruleProvider.Object);
            var result = (await function.Run(new Mock<HttpRequestMessage>().Object, 
                new Mock<DurableOrchestrationContextBase>().Object,
                "somecompany", 
                "TAS", 
                "some-non-existing-rule")).ShouldBeOfType<NotFoundObjectResult>();

            result
                .Value
                .ToString()
                .ShouldContain("Rule not found");
        }
    }
}