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
        public async Task ThrottlingRateOver2MinutesTest1()
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
                    await SimulateRequests(customerThrottle1, tfe, testCounter1).ConfigureAwait(false);
                });

                var t2 = new Task(async () =>
                {
                    await SimulateRequests(customerThrottle2, tfe, testCounter2).ConfigureAwait(false);
                    return;
                });

                var t3 = new Task(async () =>
                {
                    await SimulateRequests(customerThrottle3, tfe, testCounter3).ConfigureAwait(false);
                    return;
                });

                var t4 = new Task(async () =>
                {
                    await SimulateRequests(customerThrottle4, tfe, testCounter4).ConfigureAwait(false);
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
                    if (sw.ElapsedMilliseconds > 59999)
                        break;
                }

                tokenSource.Cancel();

                Task.WaitAll(t1, t2, t3, t4);

                sw.Stop();

                // customer 1: 1 request per second
                Assert.AreEqual(1, testCounter1.count / (sw.Elapsed.TotalSeconds +1 ), 0.05);

                // customer 2: 2 requests per second
                Assert.AreEqual(2, testCounter2.count / (sw.Elapsed.TotalSeconds + 1), 0.05);

                // customer 3: 4 requests per second
                Assert.AreEqual(4, testCounter3.count / (sw.Elapsed.TotalSeconds + 1), 0.05);

                // customer 4: 120 requests per second
                Assert.AreEqual(120, testCounter4.count / (sw.Elapsed.TotalSeconds + 1), 0.05);
            }
        }

        private async Task SimulateRequests(CustomerThrottle ct, ThrottledFunctionExecutor<TestRequest, TestResponse> tfe, TestCounter counter)
        {
            try
            {
                while (true)
                {
                    var response = await tfe.ExecuteThrottled(ct, new TestRequest()).ConfigureAwait(false);
                    counter.Add();
                }
            }
            catch (TaskCanceledException)
            { 
            }
            catch (Exception)
            { 
                throw;
            }
        }
    }
}
