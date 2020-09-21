using ImmutableObjectGraph.Generation;
using System;

namespace Banda.AsyncThrottledFunctionExecutor
{
    [GenerateImmutable]
    public partial class CustomerThrottle : IEquatable<CustomerThrottle>
    {
        readonly string customerIdentityKey;
        readonly int maximumRequestsPerSecond;

        public bool Equals(CustomerThrottle other)
        {
            if (other == null) return false;

            return customerIdentityKey == other.customerIdentityKey
                && maximumRequestsPerSecond == other.maximumRequestsPerSecond;
        }

        public void ValidateArguments()
        {
            if (customerIdentityKey == null)
                throw new ArgumentNullException(nameof(customerIdentityKey));
            if (string.IsNullOrWhiteSpace(customerIdentityKey))
                throw new ArgumentOutOfRangeException(nameof(customerIdentityKey));
            if (maximumRequestsPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumRequestsPerSecond));
        }
    }
}
