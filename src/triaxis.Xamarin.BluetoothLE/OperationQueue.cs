using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace triaxis.Xamarin.BluetoothLE
{
    partial class OperationQueue
    {
        IOperation _current;
        Task _last = Task.CompletedTask;
        ILogger _logger;
        TaskScheduler _scheduler;

        public OperationQueue(ILogger logger, TaskScheduler scheduler = null)
        {
            _logger = logger;
            _scheduler = scheduler ?? TaskScheduler.FromCurrentSynchronizationContext();
        }

        public Task<T> Enqueue<T>(IOperation<T> op, int timeout = Timeout.Infinite)
        {
            _logger.LogDebug("Enqueuing {Operation}", op);
            var prev = _last;
            var t = op.Task;
            _last = t;
            prev.ContinueWith(async (tPrev) =>
            {
                _current = op;
                int delay = op.StartDelay;
                if (delay > 0)
                {
                    _logger.LogDebug("{Operation} pre-delay {DelayMilliseconds} ms", op, delay);
                    await Task.Delay(delay);
                }

                try
                {
                    if (timeout != Timeout.Infinite)
                    {
                        _logger.LogDebug("{Operation} starting with {TimeoutMilliseconds} ms timeout", op, timeout);
                        var cts = new CancellationTokenSource(timeout);
                        op.Start(cts.Token);
                        _ = t.ContinueWith(_ =>
                        {
                            cts.Dispose();
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    else
                    {
                        _logger.LogDebug("{Operation} starting without timeout", op);
                        op.Start(default);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{Operation} crashed on start", op);
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
    }
}
