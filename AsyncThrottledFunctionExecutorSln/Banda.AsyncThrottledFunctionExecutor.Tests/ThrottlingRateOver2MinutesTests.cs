using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncThrottledFunctionExecutor.Tests
{
    [TestClass]
    public class ThrottlingRateOver2MinutesTests
    {
        [TestMethod]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task ThrottlingRateOver2MinutesTest1()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Func<TestRequest, Task<TestResponse>> executingFunc = async (req) =>
            {
                return await Task.Run(() => { return new TestResponse(); }).ConfigureAwait(false);
            };

            // customer 1: 1 request per second
            CustomerThrottle customerThrottle1 = CustomerThrottle.Create("1", 1);
            // customer 2: 2 requests per second
            CustomerThrottle customerThrottle2 = CustomerThrottle.Create("2", 2);
            // customer 3: 4 requests per second
            CustomerThrottle customerThrottle3 = CustomerThrottle.Create("3", 4);
            // customer 4: 120 requests per second
            CustomerThrottle customerThrottle4 = CustomerThrottle.Create("4", 120);

            TestCounter testCounter1 = new TestCounter();
            TestCounter testCounter2 = new TestCounter();
            TestCounter testCounter3 = new TestCounter();
            TestCounter testCounter4 = new TestCounter();

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            using (ThrottledFunctionExecutor<TestRequest, TestResponse> tfe = new ThrottledFunctionExecutor<TestRequest, TestResponse>
                (executingFunc, tokenSource.Token))
            {

                var t1 = new Task(async () =>
                {
                    await SimulateRequestsAsync(customerThrottle1, tfe, testCounter1).ConfigureAwait(false);
                });

                var t2 = new Task(async () =>
                {
                    await SimulateRequestsAsync(customerThrottle2, tfe, testCounter2).ConfigureAwait(false);
                    return;
                });

                var t3 = new Task(async () =>
                {
                    await SimulateRequestsAsync(customerThrottle3, tfe, testCounter3).ConfigureAwait(false);
                    return;
                });

                var t4 = new Task(async () =>
                {
                    await SimulateRequestsAsync(customerThrottle4, tfe, testCounter4).ConfigureAwait(false);
                    return;
                });

                Stopwatch sw = new Stopwatch();
                sw.Start();

                t1.Start();
                t2.Start();
                t3.Start();
                t4.Start();

                while (true)
                {
                    Thread.Sleep(1);
                    if (sw.Elapsed.TotalMilliseconds >= 59950)
                        break;
                }

                testCounter1.stopCount = true;
                testCounter2.stopCount = true;
                testCounter3.stopCount = true;
                testCounter4.stopCount = true;

                sw.Stop();

                tokenSource.Cancel();

                Task.WaitAll(t1, t2, t3, t4);

                // customer 1: 1 request per second
                Assert.AreEqual(1, testCounter1.count / sw.Elapsed.TotalSeconds, 0.05);

                // customer 2: 2 requests per second
                Assert.AreEqual(2, testCounter2.count / sw.Elapsed.TotalSeconds, 0.10);

                // customer 3: 4 requests per second
                Assert.AreEqual(4, testCounter3.count / sw.Elapsed.TotalSeconds, 0.4);

                // customer 4: 120 requests per second
                Assert.AreEqual(120, testCounter4.count / sw.Elapsed.TotalSeconds, 12);
            }
        }

        private async Task SimulateRequestsAsync(CustomerThrottle ct, ThrottledFunctionExecutor<TestRequest, TestResponse> tfe, TestCounter counter)
        {
            try
            {
                while (true)
                {
                    var response = await tfe.ExecuteThrottledAsync(ct, new TestRequest()).ConfigureAwait(false);
                    counter.Add();
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
