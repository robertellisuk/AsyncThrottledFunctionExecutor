using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Banda.AsyncThrottledFunctionExecutor
{
    internal static class Extensions
    {
        public static bool IsNotNone(this CancellationToken cancellationToken)
        {
            return !CancellationToken.Equals(CancellationToken.None, cancellationToken);
        }
    }
}
