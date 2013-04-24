using System;
using System.Threading;

namespace MVVM.Core.Optimization
{
    /// <summary>
    /// Throttle UI optimization technique, which executes last action
    /// once <param name="timeout" /> milliseconds passed since last call.
    /// All intermediate actions will be ignored (canceled) as outdated.
    /// </summary>
    public class UIThrottle
    {
        #region Fields

        private readonly object _throttleLock = new object();
        private readonly SynchronizationContext _syncContext;

        private readonly int _timeout;

        private DateTime? _lastActionTimestamp;
        private Action _lastAction;

        #endregion

        public UIThrottle(int timeout)
        {
            if (timeout <= 0) throw new ArgumentOutOfRangeException("timeout", "timeout must be positive");

            _timeout = timeout;
            _syncContext = SynchronizationContext.Current;
        }

        public void Handle(Action action)
        {
            lock (_throttleLock)
            {
                var hasPendingRequest = _lastActionTimestamp.HasValue;

                _lastActionTimestamp = DateTime.Now;
                _lastAction = action;

                if (hasPendingRequest)
                {
                    return;
                }
            }

            ThreadPool.QueueUserWorkItem(o =>
            {
                while (true)
                {
                    DateTime? sleepTimestamp;
                    int adjustedTimeout = _timeout;

                    lock (_throttleLock)
                    {
                        adjustedTimeout = (int)(_lastActionTimestamp.Value.AddMilliseconds(_timeout) - DateTime.Now).TotalMilliseconds;
                        sleepTimestamp = _lastActionTimestamp;
                    }

                    if (adjustedTimeout > 0)
                    {
                        Thread.Sleep(adjustedTimeout);
                    }

                    lock (_throttleLock)
                    {
                        if (_lastActionTimestamp.Value == sleepTimestamp.Value)
                        {
                            Execute(_lastAction);

                            _lastActionTimestamp = null;
                            _lastAction = null;

                            break;
                        }
                    }

                    Thread.Sleep(10);
                }
            });
        }

        private void Execute(Action action)
        {
            if (_syncContext != null)
            {
                _syncContext.Send(s => action(), null);
            }
            else
            {
                action();
            }
        }
    }
}
