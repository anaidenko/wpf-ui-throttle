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

        private static readonly int LOCK_TIMEOUT = 2000;
        private static readonly int SLEEP_INTERVAL = 10;

        private readonly object _throttleLock = new object();
        private readonly SynchronizationContext _syncContext;

        private readonly int _throttleTimeout;

        private DateTime? _lastActionTimestamp;
        private Action _lastAction;

        #endregion

        public UIThrottle(int throttleTimeout)
        {
            if (throttleTimeout <= 0) throw new ArgumentOutOfRangeException("throttleTimeout", "throttleTimeout must be positive");

            _throttleTimeout = throttleTimeout;
            _syncContext = SynchronizationContext.Current;
        }

        public void Handle(Action action)
        {
            using (var locking = new SafeResourceLock(_throttleLock, LOCK_TIMEOUT))
            {
                if (locking.IsTimedOut)
                {
                    return;
                }

                var hasPendingRequest = _lastActionTimestamp.HasValue;

                _lastActionTimestamp = DateTime.Now;
                _lastAction = action;

                if (hasPendingRequest)
                {
                    return;
                }
            }

            ThreadPool.QueueUserWorkItem(o => ProcessThrottleRequest());
        }

        private void ProcessThrottleRequest()
        {
            try
            {
                while (true)
                {
                    DateTime? sleepTimestamp;
                    int adjustedTimeout = _throttleTimeout;

                    using (var locking = new SafeResourceLock(_throttleLock, LOCK_TIMEOUT))
                    {
                        if (locking.IsTimedOut)
                        {
                            Thread.Sleep(SLEEP_INTERVAL);
                            continue;
                        }

                        adjustedTimeout = (int)(_lastActionTimestamp.Value.AddMilliseconds(_throttleTimeout) - DateTime.Now).TotalMilliseconds;
                        sleepTimestamp = _lastActionTimestamp;
                    }

                    if (adjustedTimeout > 0)
                    {
                        Thread.Sleep(adjustedTimeout);
                    }

                    using (var locking = new SafeResourceLock(_throttleLock, LOCK_TIMEOUT))
                    {
                        if (locking.IsTimedOut)
                        {
                            Thread.Sleep(SLEEP_INTERVAL);
                            continue;
                        }

                        if (_lastActionTimestamp.Value == sleepTimestamp.Value)
                        {
                            // call asynchronously to prevent deadlocks (acquiring locked _throttleLock on UI thread).
                            ExecuteAsync(() =>
                            {
                                try
                                {
                                    _lastAction();
                                }
                                // protection from potential exceptions in _lastAction delegate, to be sure we reset throttle state.
                                finally
                                {
                                    _lastActionTimestamp = null;
                                    _lastAction = null;
                                }
                            });

                            // finish throttle thread work item.
                            return;
                        }
                    }

                    Thread.Sleep(SLEEP_INTERVAL);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ExecuteAsync(Action action)
        {
            if (_syncContext != null)
            {
                _syncContext.Post(s => action(), null);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(o => action());
            }
        }
    }
}
