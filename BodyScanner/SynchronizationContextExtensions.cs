using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace BodyScanner
{
    static class SynchronizationContextExtensions
    {
        public static void Post(this SynchronizationContext context, Action action)
        {
            Contract.Requires(context != null);

            context.Post(_ => action.Invoke(), null);
        }

        public static void Send(this SynchronizationContext context, Action action)
        {
            Contract.Requires(context != null);

            context.Send(_ => action.Invoke(), null);
        }
    }
}
