using System;
using System.Collections.Generic;
using System.Text;

namespace Banda.AsyncThrottledFunctionExecutor.Tests
{
    public class TestCounter
    {
        public long count = 0;
        public bool stopCount = false;

        public void Add()
        {
            if (stopCount)
            {
                return;
            }

            System.Threading.Interlocked.Increment(ref count);
        }
    }
}
