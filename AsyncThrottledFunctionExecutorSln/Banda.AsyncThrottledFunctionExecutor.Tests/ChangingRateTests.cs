using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncThrottledFunctionExecutor.Tests
{
    [TestClass]
    public class ChangingRateTests
    {
        [TestMethod]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task ChangingRateTest1()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Func<TestRequest, Task<TestResponse>> executingFunc = async (req) =>
            {
                return await Task.Run(() => { return new TestResponse(); }).ConfigureAwait(false);
            };

            // customer 1: Initially 1 request per second
            CustomerThrottle customerThrottle1rate1 = CustomerThrottle.Create("1", 1);
            //  ... then 60 request per second
            CustomerThrottle customerThrottle1rate2 = CustomerThrottle.Create("1", 60);

            TestCounter testCounter1 = new TestCounter();

            CancellationTokenSource tfeTokenSource = new CancellationTokenSource();

            using (ThrottledFunctionExecutor<TestRequest, TestResponse> tfe = new ThrottledFunctionExecutor<TestRequest, TestResponse>
                (executingFunc, tfeTokenSource.Token))
            {
                CancellationTokenSource cts1 = new CancellationTokenSource();
                CancellationTokenSource cts2 = new CancellationTokenSource();

                var t1 = new Task(async () =>
                {
                    await SimulateRequestsAsync(customerThrottle1rate1, tfe, testCounter1, cts1.Token).ConfigureAwait(false);
                });

                var t2 = new Task(async () =>
                {
                    await SimulateRequestsAsync(customerThrottle1rate2, tfe, testCounter1, cts2.Token).ConfigureAwait(false);
                    return;
                });


                Stopwatch sw1 = new Stopwatch();
                Stopwatch sw2 = new Stopwatch();
                                
                t1.Start();

                sw1.Start();

                while (true)
                {
                    Thread.Sleep(1);
                    if (sw1.Elapsed.TotalMilliseconds >= 59750)
                        break;
                }

                testCounter1.stopCount = true;
                sw1.Stop();

                cts1.Cancel();
                
                Task.WaitAll(t1);

                t2.Start();

                sw2.Start();
                testCounter1.stopCount = false;

                while (true)
                {
                    Thread.Sleep(1);
                    if (sw2.Elapsed.TotalMilliseconds >= 59750)
                        break;
                }

                testCounter1.stopCount = true;
                sw2.Stop();

                cts2.Cancel();

                Task.WaitAll(t2);

               // Total requests = 60 + 3600 = 3660
                // Total seconds = ~119.5
                // r/s = 30.63

                TimeSpan totalElapsed = sw1.Elapsed + sw2.Elapsed;

                Assert.AreEqual(30.63, testCounter1.count / totalElapsed.TotalSeconds, 0.75);
            }
        }

        private async Task SimulateRequestsAsync(CustomerThrottle ctRate,
            ThrottledFunctionExecutor<TestRequest, TestResponse> tfe, TestCounter counter, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var response = await tfe.ExecuteThrottledAsync(ctRate, new TestRequest()).ConfigureAwait(false);

                    counter.Add();

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
