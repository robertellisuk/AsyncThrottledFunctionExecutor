using Banda.AsyncTokenBucket;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncThrottledFunctionExecutor
{
    public class ThrottledFunctionExecutor<TRequest, TResponse> : IDisposable
    {
        private readonly Func<TRequest, CancellationToken, Task<TResponse>> delegateFunction;
        
        private readonly CancellationToken cancellationToken;

        private readonly ConcurrentDictionary<string, CustomerThrottleContext> contextDict
            = new ConcurrentDictionary<string, CustomerThrottleContext>();
                
        private ThrottledFunctionExecutor() { }
                
        public ThrottledFunctionExecutor(Func<TRequest, CancellationToken, Task<TResponse>> delegateFunction)
        {
            this.delegateFunction = delegateFunction;
            this.cancellationToken = CancellationToken.None;
        }

        public ThrottledFunctionExecutor(Func<TRequest, CancellationToken, Task<TResponse>> delegateFunction,
            CancellationToken cancellationToken)
        {
            this.delegateFunction = delegateFunction;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Executes the delegate function on a throttled basis. 
        /// 
        /// When this overload is called and no CancellationToken was passed at instance construction, 
        ///  then no internal cancellation logic will be applied.
        /// When this overload is called and a CancellationToken was passed at instance construction, 
        ///  then that CancellationToken will be used for internal cancellation logic and will also be passed to the delegate.
        /// </summary>
        /// <param name="customerThrottle">Determines which throttle-rate to use. If you pass a customerThrottle with an ID that hasn't been seen previously by the instance,
        /// then a new underlying TokenBucket is created. If you pass a customerThrottle with an ID that has been seen previously by the instance, then you'll get the
        /// underlying TokenBucket already associated with that ID.
        /// If you want to change the throttling rate associated with a particular ID, then pass in a new customerThrottle object with the new maximumRequestsPerSecond property 
        /// value and the same ID.</param>
        /// <param name="request">The request object (of type TRequest) to pass to the delegate.</param>
        /// <returns></returns>
        public async Task<TResponse> ExecuteThrottledAsync(CustomerThrottle customerThrottle, TRequest request)
        {
            CheckIfThisInstanceIsDisposed_ForPlacementAtTopOfPublicMethods();

            if (customerThrottle == null)
            {
                throw new ArgumentNullException(nameof(customerThrottle));
            }

            customerThrottle.ValidateArguments();

            if (this.cancellationToken.IsNotNone())
            {
                return await ExecuteThrottledAsync(customerThrottle, request, this.cancellationToken, this.cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await ExecuteThrottledAsync(customerThrottle, request, CancellationToken.None, CancellationToken.None).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Executes the delegate function on a throttled basis. 
        /// 
        /// When this overload is called and no CancellationToken was passed at instance construction,  
        ///  then the internal cancellation logic will use the CancellationToken passed to this method, and the same CancellationToken will be passed to the delegate.
        /// When this overload is called and a CancellationToken was passed at instance construction, 
        ///  then the internal cancellation logic will use the instance CancellationToken, and the CancellationToken passed to this overload will be passed to the delegate.
        /// </summary>
        /// <param name="customerThrottle">Determines which throttle-rate to use. If you pass a customerThrottle with an ID that hasn't been seen previously by the instance,
        /// then a new underlying TokenBucket is created. If you pass a customerThrottle with an ID that has been seen previously by the instance, then you'll get the
        /// underlying TokenBucket already associated with that ID.
        /// If you want to change the throttling rate associated with a particular ID, just pass in a new customerThrottle object with the new maximumRequestsPerSecond property 
        /// value and the same ID.</param>
        /// <param name="request">The request object (of type TRequest) to pass to the delegate.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TResponse> ExecuteThrottledAsync(CustomerThrottle customerThrottle, TRequest request, CancellationToken cancellationToken)
        {
            CheckIfThisInstanceIsDisposed_ForPlacementAtTopOfPublicMethods();

            if (customerThrottle == null)
            {
                throw new ArgumentNullException(nameof(customerThrottle));
            }

            customerThrottle.ValidateArguments();

            if (this.cancellationToken.IsNotNone()) 
            {
                return await ExecuteThrottledAsync(customerThrottle, request, this.cancellationToken, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await ExecuteThrottledAsync(customerThrottle, request, cancellationToken, cancellationToken).ConfigureAwait(false);
            }
        }
                
        private async Task<TResponse> ExecuteThrottledAsync(CustomerThrottle customerThrottle, TRequest request, CancellationToken cancellationTokenToCheckInternally, 
            CancellationToken cancellationTokenToPassToDelegate)
        {
            ITokenBucket tokenBucket = null;

            if (customerThrottle.MaximumRequestsPerSecond > 0)
            {
                tokenBucket = await RetrieveMatchingTokenBucketContextOrMakeNew(customerThrottle).ConfigureAwait(false);

                if (cancellationTokenToCheckInternally.IsNotNone())
                {
                    cancellationTokenToCheckInternally.ThrowIfCancellationRequested();
                }

                await tokenBucket.WaitConsumeAsync(1, cancellationTokenToCheckInternally).ConfigureAwait(false);
            }

            if (cancellationTokenToCheckInternally.IsNotNone())
            {
                cancellationTokenToCheckInternally.ThrowIfCancellationRequested();
            }

            return await delegateFunction(request, cancellationTokenToPassToDelegate).ConfigureAwait(false);
        }

        private async Task<ITokenBucket> RetrieveMatchingTokenBucketContextOrMakeNew(CustomerThrottle customerThrottle)
        {
            ITokenBucket tokenBucket;

            if (contextDict.TryGetValue(customerThrottle.CustomerIdentityKey, out var storedThrottle))
            {
                if (!storedThrottle.CustomerThrottle.Equals(customerThrottle))
                {
                    tokenBucket = await CreateAndStoreTokenBucketContext(customerThrottle).ConfigureAwait(false);
                }
                else // if storedThrottle.CustomerThrottle.Equals(customerThrottle)
                {
                    tokenBucket = (ITokenBucket)storedThrottle.TokenBucket;
                }
            }
            else // (! contextDict.TryGetValue(customerThrottle.CustomerIdentityKey, out var storedThrottle))
            {
                tokenBucket = await CreateAndStoreTokenBucketContext(customerThrottle).ConfigureAwait(false);
            }

            return tokenBucket;
        }

        private async Task<ITokenBucket> CreateAndStoreTokenBucketContext(CustomerThrottle customerThrottle)
        {
            object objNewTokenBucket = await TokenBucketFactory.GetTokenBucketAsync(customerThrottle).ConfigureAwait(false);

            CustomerThrottleContext newThrottleContext = CustomerThrottleContext.Create(
                customerThrottle: customerThrottle,
                tokenBucket: objNewTokenBucket);

            contextDict.AddOrUpdate(customerThrottle.CustomerIdentityKey, newThrottleContext,
                updateValueFactory: (factoryKey, factoryValue) =>
                {
                    return newThrottleContext;
                });

            return (ITokenBucket)objNewTokenBucket;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void CheckIfThisInstanceIsDisposed_ForPlacementAtTopOfPublicMethods()
        {
            if (disposedValue)
                throw new ObjectDisposedException(nameof(ThrottledFunctionExecutor<TRequest, TResponse>));
        }

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
