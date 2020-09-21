using ImmutableObjectGraph.Generation;

namespace Banda.AsyncThrottledFunctionExecutor
{
    [GenerateImmutable]
    internal partial class CustomerThrottleContext
    {
        readonly CustomerThrottle customerThrottle;

        readonly object tokenBucket;
    }
}
