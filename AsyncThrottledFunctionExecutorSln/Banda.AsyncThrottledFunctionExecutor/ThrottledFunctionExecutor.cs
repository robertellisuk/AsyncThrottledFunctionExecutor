using Banda.AsyncTokenBucket;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncThrottledFunctionExecutor
{
    public class ThrottledFunctionExecutor<TRequest, TResponse> : IDisposable
    {
        private readonly Func<TRequest, Task<TResponse>> executingFunc;
        private readonly CancellationToken cancellationToken;

        private readonly ConcurrentDictionary<string, CustomerThrottleContext> contextDict
            = new ConcurrentDictionary<string, CustomerThrottleContext>();

        private ThrottledFunctionExecutor() { }

        public ThrottledFunctionExecutor(Func<TRequest, Task<TResponse>> executingFunc,
            CancellationToken cancellationToken)
        {
            this.executingFunc = executingFunc;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Executes the executingFunc delegate on a throttled basis
        /// </summary>
        /// <param name="customerThrottle">Determines which throttle-rate to use. If you pass a customerThrottle with an ID that hasn't been seen previously by the instance,
        /// then a new underlying TokenBucket is created. If you pass a customerThrottle with an ID that has been seen previously by the instance, then you'll get the
        /// underlying TokenBucket already associated with that ID.
        /// If you want to change the throttling rate associated with a particular ID, just pass in a new customerThrottle object with the new maximumRequestsPerSecond property 
        /// value and the same ID.</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TResponse> ExecuteThrottledAsync(CustomerThrottle customerThrottle, TRequest request)
        {
            if (customerThrottle == null)
                throw new ArgumentNullException(nameof(customerThrottle));

            customerThrottle.ValidateArguments();

                Func<CustomerThrottle, Task<ITokenBucket>> makeNewContextAndStore = async (context) =>
                {
                    object objNewTokenBucket = await TokenBucketFactory.GetTokenBucketAsync(context).ConfigureAwait(false);

                    CustomerThrottleContext newThrottleContext = CustomerThrottleContext.Create(
                        customerThrottle: context,
                        tokenBucket: objNewTokenBucket);

                    contextDict.AddOrUpdate(context.CustomerIdentityKey, newThrottleContext,
                        updateValueFactory: (factoryKey, factoryValue) =>
                        {
                            return newThrottleContext;
                        });

                    return (ITokenBucket)objNewTokenBucket;
                };

            ITokenBucket tokenBucket;

            if (contextDict.TryGetValue(customerThrottle.CustomerIdentityKey, out var storedThrottle))
            {
                if (! storedThrottle.CustomerThrottle.Equals(customerThrottle))
                {
                    tokenBucket = await makeNewContextAndStore(customerThrottle).ConfigureAwait(false);
                }
                else // if storedThrottle.CustomerThrottle.Equals(customerThrottle)
                {
                    tokenBucket = (ITokenBucket)storedThrottle.TokenBucket;
                }
            }
            else // (! contextDict.TryGetValue(customerThrottle.CustomerIdentityKey, out var storedThrottle))
            {
                tokenBucket = await makeNewContextAndStore(customerThrottle).ConfigureAwait(false);
            }

            if (customerThrottle.MaximumRequestsPerSecond > 0)
            {
                await tokenBucket.WaitConsumeAsync(1, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await executingFunc(request).ConfigureAwait(false); 
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (contextDict != null)
                    {
                        contextDict.Clear();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ThrottledFunctionExecutor()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
