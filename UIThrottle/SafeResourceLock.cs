using System;
using System.Threading;

namespace MVVM.Core.Optimization
{
    internal class SafeResourceLock : IDisposable
    {
        private readonly object _syncObject;

        public SafeResourceLock(object syncObject, int timeout)
        {
            _syncObject = syncObject;
            IsTimedOut = !Monitor.TryEnter(syncObject, timeout);
        }

        public bool IsTimedOut { get; private set; }

        public void Dispose()
        {
            if (!IsTimedOut)
            {
                Monitor.Exit(_syncObject);
            }
        }
    }
}
