// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using ImmutableObjectGraph.Generation;
using System;

namespace Banda.AsyncThrottledFunctionExecutor
{
    partial class CustomerThrottle
    {
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        private static readonly CustomerThrottle DefaultInstance = GetDefaultTemplate();
        private static int lastIdentityProduced;
        [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly uint identity;
        [System.ObsoleteAttribute("This constructor for use with deserializers only. Use the static Create factory method instead.")]
        public CustomerThrottle(System.String CustomerIdentityKey, System.Int32 MaximumRequestsPerSecond): this(NewIdentity(), customerIdentityKey: CustomerIdentityKey, maximumRequestsPerSecond: MaximumRequestsPerSecond, skipValidation: false)
        {
        }

        protected CustomerThrottle(uint identity, System.String customerIdentityKey, System.Int32 maximumRequestsPerSecond, bool skipValidation)
        {
            this.identity = identity;
            this.customerIdentityKey = customerIdentityKey;
            this.maximumRequestsPerSecond = maximumRequestsPerSecond;
            if (!skipValidation)
            {
                this.Validate();
            }
        }

        public string CustomerIdentityKey => this.customerIdentityKey;
        public int MaximumRequestsPerSecond => this.maximumRequestsPerSecond;
        internal protected uint Identity => this.identity;
        public static CustomerThrottle Create(ImmutableObjectGraph.Optional<System.String> customerIdentityKey = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> maximumRequestsPerSecond = default(ImmutableObjectGraph.Optional<System.Int32>))
        {
            var identity = ImmutableObjectGraph.Optional.For(NewIdentity());
            return DefaultInstance.WithFactory(customerIdentityKey: ImmutableObjectGraph.Optional.For(customerIdentityKey.GetValueOrDefault(DefaultInstance.CustomerIdentityKey)), maximumRequestsPerSecond: ImmutableObjectGraph.Optional.For(maximumRequestsPerSecond.GetValueOrDefault(DefaultInstance.MaximumRequestsPerSecond)), identity: identity);
        }

        public CustomerThrottle With(ImmutableObjectGraph.Optional<System.String> customerIdentityKey = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> maximumRequestsPerSecond = default(ImmutableObjectGraph.Optional<System.Int32>))
        {
            return (CustomerThrottle)this.WithCore(customerIdentityKey: customerIdentityKey, maximumRequestsPerSecond: maximumRequestsPerSecond);
        }

        static protected uint NewIdentity()
        {
            return (uint)System.Threading.Interlocked.Increment(ref lastIdentityProduced);
        }

        protected virtual CustomerThrottle WithCore(ImmutableObjectGraph.Optional<System.String> customerIdentityKey = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> maximumRequestsPerSecond = default(ImmutableObjectGraph.Optional<System.Int32>))
        {
            return this.WithFactory(customerIdentityKey: customerIdentityKey, maximumRequestsPerSecond: maximumRequestsPerSecond, identity: ImmutableObjectGraph.Optional.For(this.Identity));
        }

        static partial void CreateDefaultTemplate(ref Template template);
        private static CustomerThrottle GetDefaultTemplate()
        {
            var template = new Template();
            CreateDefaultTemplate(ref template);
            return new CustomerThrottle(default(uint), template.CustomerIdentityKey, template.MaximumRequestsPerSecond, skipValidation: true);
        }

        partial void Validate();
        private CustomerThrottle WithFactory(ImmutableObjectGraph.Optional<System.String> customerIdentityKey = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> maximumRequestsPerSecond = default(ImmutableObjectGraph.Optional<System.Int32>), ImmutableObjectGraph.Optional<uint> identity = default(ImmutableObjectGraph.Optional<uint>))
        {
            if ((identity.IsDefined && identity.Value != this.Identity) || (customerIdentityKey.IsDefined && customerIdentityKey.Value != this.CustomerIdentityKey) || (maximumRequestsPerSecond.IsDefined && maximumRequestsPerSecond.Value != this.MaximumRequestsPerSecond))
            {
                return new CustomerThrottle(identity: identity.GetValueOrDefault(this.Identity), customerIdentityKey: customerIdentityKey.GetValueOrDefault(this.CustomerIdentityKey), maximumRequestsPerSecond: maximumRequestsPerSecond.GetValueOrDefault(this.MaximumRequestsPerSecond), skipValidation: false);
            }
            else
            {
                return this;
            }
        }

#pragma warning disable 649 // field initialization is optional in user code

        private struct Template
        {
            internal System.String CustomerIdentityKey;
            internal System.Int32 MaximumRequestsPerSecond;
        }
#pragma warning restore 649
    }
}