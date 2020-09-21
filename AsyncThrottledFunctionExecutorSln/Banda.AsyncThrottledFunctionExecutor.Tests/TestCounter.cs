using System;
using System.Collections.Generic;
using System.Text;

namespace Banda.AsyncThrottledFunctionExecutor.Tests
{
    public class TestCounter
    {
        public long count = 0;

        public void Add()
        {
            System.Threading.Interlocked.Increment(ref count);
        }
    }
}
