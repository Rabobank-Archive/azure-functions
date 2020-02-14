using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;

namespace Functions.Helpers
{
    public class PoliciesResolver : IPoliciesResolver
    {
        private const int CacheExpirationInMinutes = 5;
        private readonly IVstsRestClient _client;
        private readonly IMemoryCache _memoryCache;

        public PoliciesResolver(IVstsRestClient client, IMemoryCache memoryCache)
        {
            _client = client;
            _memoryCache = memoryCache;
        }

        public IEnumerable<MinimumNumberOfReviewersPolicy> Resolve(string projectId)
        {
            if (_memoryCache.TryGetValue<IEnumerable<MinimumNumberOfReviewersPolicy>>(projectId, out var policies))
                return policies;

            policies = _client.Get(SecurePipelineScan.VstsService.Requests.Policies.MinimumNumberOfReviewersPolicies(projectId));

            return _memoryCache.Set(projectId,
                                    policies,
                                    new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(CacheExpirationInMinutes)));
        }
    }
}