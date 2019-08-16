using System.Linq;
using AutoFixture;
using Functions.Activities;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GetHooksActivityTests
    {
        private readonly Fixture _fixture;

        public GetHooksActivityTests()
        {
            _fixture = new Fixture();
        }
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void ReturnsHooksFromRequest(int hookCount) 
        {
            // Arrange
            var context = new Mock<DurableActivityContextBase>();
            var client = new Mock<IVstsRestClient>();
            var hooks = _fixture.CreateMany<Hook>(hookCount).ToList();
            client.Setup(c => c.Get(It.IsAny<IEnumerableRequest<Hook>>()))
                .Returns(hooks);
            
            // Act
            var fun = new GetHooksActivity(client.Object);
            var result = fun.Run(context.Object);
            
            // Assert
            result.Union(hooks).Count().ShouldBe(hookCount);
        }
    }
}