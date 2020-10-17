using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncThrottledFunctionExecutor.Tests
{
    [TestClass]
    public class CancellationTests
    {
        /// When the overload having no CancellationToken argument is called, and a CancellationToken was passed at instance construction, 
        ///  then that CancellationToken will be used for internal cancellation logic and will also be passed to the delegate.
        ///  For this case, 
        ///  -prove that the instance CT can be used to cancel the task on the delegate (case #1);
        ///  -prove that the instance CT is used for the internal cancellation logic as expected (case #2).
        ///  
        [TestMethod]
        public async Task CancellationTestCase1()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            TaskCompletionSource<bool> taskCompletionSourceSetWhenDelegateRuns = new TaskCompletionSource<bool>();

            Func<TaskCompletionSource<bool>, CancellationToken, Task<TestResponse>> funcWhichIsAFairlyLongRunningTask = async (req, token) =>
            {
                req.SetResult(true);

                await Task.Delay(TimeSpan.FromMinutes(3).Duration(), token).ConfigureAwait(false);

                return new TestResponse();
            };

            using (ThrottledFunctionExecutor<TaskCompletionSource<bool>, TestResponse> tfe = new ThrottledFunctionExecutor<TaskCompletionSource<bool>, TestResponse>
                                    (funcWhichIsAFairlyLongRunningTask, cancellationToken))
            {
                CustomerThrottle customerThrottle = CustomerThrottle.Create("ExampleKey", 1);

                Task<TestResponse> response = tfe.ExecuteThrottledAsync(customerThrottle, taskCompletionSourceSetWhenDelegateRuns);

                await taskCompletionSourceSetWhenDelegateRuns.Task.ConfigureAwait(false);
                
                // the delegate is running at this point, and so all of the internal cancellation logic has been cleared

                await Task.Delay(TimeSpan.FromSeconds(5).Duration()).ConfigureAwait(false); // just to ensure we've hit the Task.Delay in the delegate

                try
                {
                    cancellationTokenSource.Cancel();

                    var result = await response.ConfigureAwait(false);

                    Assert.Fail(); // OperationCanceledException should have been thrown 
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    Assert.Fail();
                }

                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task CancellationTestCase2()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            cancellationTokenSource.Cancel();

            Func<TestRequest, CancellationToken, Task<TestResponse>> funcPlaceholderDelegateThatWillNeverRun = async (req, token) =>
            {
                await Task.Delay(1).ConfigureAwait(false);

                throw new NotImplementedException();
            };

            using (ThrottledFunctionExecutor<TestRequest, TestResponse> tfe = new ThrottledFunctionExecutor<TestRequest, TestResponse>
                         (funcPlaceholderDelegateThatWillNeverRun, cancellationToken))
            {
                CustomerThrottle customerThrottle = CustomerThrottle.Create("ExampleKey", 1);

                try
                {
                    TestResponse response = await tfe.ExecuteThrottledAsync(customerThrottle, new TestRequest()).ConfigureAwait(false);

                    Assert.Fail();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    Assert.Fail();
                }

                Assert.Fail();
            }
        }

        /// When the overload with a CancellationToken argument is called, and a CancellationToken was passed at instance construction, 
        ///  then:
        ///     - The internal cancellation logic will use the instance-level CT; and 
        ///     - The delegate will be passed the CT that was passed as an argument.
        ///  For this case, 
        ///  -prove that the instance CT is being used by the internal cancellation logic (case #3);
        ///  -prove that the delegate is passed the CT that was passed in to overload (case #4).
        [TestMethod]
        public async Task CancellationTestCase3()
        {
            CancellationTokenSource instanceCancellationTokenSource = new CancellationTokenSource();
            CancellationToken instanceCancellationToken = instanceCancellationTokenSource.Token;

            CancellationTokenSource delegateCancellationTokenSource = new CancellationTokenSource();
            CancellationToken delegateCancellationToken = delegateCancellationTokenSource.Token;

            instanceCancellationTokenSource.Cancel();

            Func<TestRequest, CancellationToken, Task<TestResponse>> funcPlaceholderDelegateThatWillNeverRun = async (req, token) =>
            {
                await Task.Delay(1).ConfigureAwait(false);

                throw new NotImplementedException();
            };

            using (ThrottledFunctionExecutor<TestRequest, TestResponse> tfe = new ThrottledFunctionExecutor<TestRequest, TestResponse>
                         (funcPlaceholderDelegateThatWillNeverRun, instanceCancellationToken))
            {
                CustomerThrottle customerThrottle = CustomerThrottle.Create("ExampleKey", 1);

                try
                {
                    TestResponse response = await tfe.ExecuteThrottledAsync(customerThrottle, new TestRequest(),
                        delegateCancellationToken).ConfigureAwait(false);

                    Assert.Fail();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    Assert.Fail();
                }

                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task CancellationTestCase4()
        {
            CancellationTokenSource instanceCancellationTokenSource = new CancellationTokenSource();
            CancellationToken instanceCancellationToken = instanceCancellationTokenSource.Token;

            CancellationTokenSource delegateCancellationTokenSource = new CancellationTokenSource();
            CancellationToken delegateCancellationToken = delegateCancellationTokenSource.Token;

            TaskCompletionSource<bool> taskCompletionSourceSetWhenDelegateRuns = new TaskCompletionSource<bool>();

            Func<TaskCompletionSource<bool>, CancellationToken, Task<TestResponse>> funcWhichIsAFairlyLongRunningTask = async (req, token) =>
            {
                req.SetResult(true);

                await Task.Delay(TimeSpan.FromMinutes(3).Duration(), token).ConfigureAwait(false);

                return new TestResponse();
            };

            using (ThrottledFunctionExecutor<TaskCompletionSource<bool>, TestResponse> tfe = new ThrottledFunctionExecutor<TaskCompletionSource<bool>, TestResponse>
                                    (funcWhichIsAFairlyLongRunningTask, instanceCancellationToken))
            {
                CustomerThrottle customerThrottle = CustomerThrottle.Create("ExampleKey", 1);

                Task<TestResponse> response = tfe.ExecuteThrottledAsync(customerThrottle, taskCompletionSourceSetWhenDelegateRuns, 
                    delegateCancellationToken);

                await taskCompletionSourceSetWhenDelegateRuns.Task.ConfigureAwait(false);

                // the delegate is running at this point, and so all of the internal cancellation logic has been cleared

                await Task.Delay(TimeSpan.FromSeconds(5).Duration()).ConfigureAwait(false); // just to ensure we've hit the Task.Delay in the delegate

                try
                {
                    delegateCancellationTokenSource.Cancel();

                    var result = await response.ConfigureAwait(false);

                    Assert.Fail(); // OperationCanceledException should have been thrown 
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    Assert.Fail();
                }

                Assert.Fail();
            }
        }
    }
}
