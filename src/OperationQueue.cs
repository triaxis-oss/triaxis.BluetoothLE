using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace triaxis.Xamarin.BluetoothLE
{
    partial class OperationQueue
    {
        IOperation _current;
        Task _last = Task.CompletedTask;
        TaskScheduler _scheduler;

        public OperationQueue(TaskScheduler scheduler = null)
        {
            _scheduler = scheduler ?? TaskScheduler.FromCurrentSynchronizationContext();
        }

        public Task<T> Enqueue<T>(IOperation<T> op, int timeout = Timeout.Infinite)
        {
            DebugLog($"Enqueuing {op}");
            var prev = _last;
            var t = op.Task;
            _last = t;
            prev.ContinueWith(async (tPrev) =>
            {
                _current = op;
                int delay = op.StartDelay;
                if (delay > 0)
                {
                    DebugLog($"{op} pre-delay {delay} ms");
                    await Task.Delay(delay);
                }

                try
                {
                    if (timeout != Timeout.Infinite)
                    {
                        DebugLog($"{op} starting with {timeout} ms timeout");
                        var cts = new CancellationTokenSource(timeout);
                        op.Start(cts.Token);
                        _ = t.ContinueWith(_ =>
                        {
                            cts.Dispose();
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    else
                    {
                        DebugLog($"{op} starting without timeout");
                        op.Start(default);
                    }
                }
                catch (Exception e)
                {
                    DebugLog($"{op} crashed on start: {e}");
                    op.Abort(e);
                }
            }, _scheduler);
            return t;
        }

        public Task<T> EnqueueOnce<T>(ref Task<T> instance, IOperation<T> op)
            => instance ?? (instance = Enqueue(op));

        public Task EnqueueOnce<T>(ref Task instance, IOperation<T> op)
            => instance ?? (instance = Enqueue(op));

        public void Abort(Exception e)
            => _current?.Abort(e);

        public Task IdleAsync()
            => _last;

        public bool TryGetCurrent<T>(out T op)
        {
            if (_current is T cur)
            {
                op = cur;
                return true;
            }
            else
            {
                op = default(T);
                return false;
            }
        }

        public bool IsIdle => _last.IsCompleted;

        partial void DebugLog(string message);
    }

#if DEBUG
    partial class OperationQueue
    {
        static int s_num;
        int _num = ++s_num;

        partial void DebugLog(string message)
        {
            Debug.WriteLine(message, $"BLEOperationQueue[{_num}]");
        }
    }
#endif
}
