using System;
using System.Threading;

namespace BodyScanner
{
    class SharedCriticalSection : IDisposable
    {
        private readonly AutoResetEvent notEntered = new AutoResetEvent(true);

        public SharedCriticalSection()
        {
        }

        public void Dispose()
        {
            notEntered.Dispose();
        }

        public bool Enter()
        {
            return notEntered.WaitOne();
        }

        public bool TryEnter()
        {
            return notEntered.WaitOne(0);
        }

        public void Exit()
        {
            notEntered.Set();
        }
    }
}
