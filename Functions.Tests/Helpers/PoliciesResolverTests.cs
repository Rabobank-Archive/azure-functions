using AutoFixture;
using Functions.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Xunit;

namespace Functions.Tests.Helpers
{
    public class PoliciesResolverTests
    {
        [Fact]
        public void CanResolveAndCache()
        {
            var fixture = new Fixture();

            var policies = fixture.CreateMany<MinimumNumberOfReviewersPolicy>();
            var vstsClient = new Mock<IVstsRestClient>();
            vstsClient
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<MinimumNumberOfReviewersPolicy>>()))
                .Returns(policies);

            var cache = new MemoryCache(new MemoryCacheOptions());

            var policiesResolver = new PoliciesResolver(vstsClient.Object, cache);
            policiesResolver.Resolve("test");

            var cacheValue = cache.Get("test");
            Assert.Equal(policies, cacheValue);
        }

        [Fact]
        public void CanResolveFromCache()
        {
            var fixture = new Fixture();

            var policies = fixture.CreateMany<MinimumNumberOfReviewersPolicy>();
            var vstsClient = new Mock<IVstsRestClient>();

            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set("test", policies);

            var policiesResolver = new PoliciesResolver(vstsClient.Object, cache);
            var result = policiesResolver.Resolve("test");

            Assert.Equal(policies, result);
        }

        [Fact]
        public void CanClearCache()
        {
            var fixture = new Fixture();

            var policies = fixture.CreateMany<MinimumNumberOfReviewersPolicy>();
            var vstsClient = new Mock<IVstsRestClient>();

            var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set("test", policies);

            var policiesResolver = new PoliciesResolver(vstsClient.Object, cache);
            policiesResolver.Clear("test");

            var isFound = cache.TryGetValue("test", out var _);
            Assert.False(isFound);
        }
    }
}
