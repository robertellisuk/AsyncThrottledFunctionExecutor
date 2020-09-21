using Banda.AsyncTokenBucket;
using System;
using System.Threading.Tasks;

namespace Banda.AsyncThrottledFunctionExecutor
{
    internal static class TokenBucketFactory
    {
        private static readonly TimeSpan oneSecond = TimeSpan.FromSeconds(1);

        public static async Task<ITokenBucket> GetTokenBucket(CustomerThrottle customerThrottle)
        {
            return await TokenBuckets.BucketWithFixedIntervalRefillStrategy(
                capacity: (long)customerThrottle.MaximumRequestsPerSecond,
                refillTokens: (long)customerThrottle.MaximumRequestsPerSecond,
                period: oneSecond).ConfigureAwait(false);
        }
    }
}
