using AutoFixture;
using Functions.Activities;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Activities
{
    public class DeleteServiceHookSubscriptionActivityTests
    {
        private readonly Fixture _fixture;

        public DeleteServiceHookSubscriptionActivityTests()
        {
            _fixture = new Fixture();
        }
        
        [Fact]
        public async Task ShouldDeleteServiceHookSubscription()
        {
            // Arrange
            var client = new Mock<IVstsRestClient>();
            var context = new Mock<DurableActivityContextBase>();
            var hook = _fixture.Create<Hook>();
            context.Setup(c => c.GetInput<Hook>()).Returns(hook);
            
            // Act
            var fun = new DeleteServiceHookSubscriptionActivity(client.Object);
            await fun.Run(context.Object);
            
            // Assert
            client.Verify(c => c.DeleteAsync(It.IsAny<IVstsRequest<Hook>>()), Times.Once);
        }
    }
}