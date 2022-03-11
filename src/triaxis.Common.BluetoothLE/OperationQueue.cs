using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE
#else
namespace triaxis.Maui.BluetoothLE
#endif
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
            using var loggerScope = _logger.BeginScope(op.GetScope());
            var context = ExecutionContext.Capture();
            void RunInContext(Action a) => ExecutionContext.Run(context, _ => a(), null);

            _logger.LogDebug("Enqueuing {Operation}", op);

            var prev = _last;
            var t = op.Task;
            _last = t;

            prev = prev.ContinueWith(_ => _current = op, TaskContinuationOptions.ExecuteSynchronously);

            int delay = op.StartDelay;
            if (delay > 0)
            {
                // insert a delay task before the actual one
                prev = prev.ContinueWith(_ =>
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        RunInContext(() => _logger.LogDebug("{Operation} pre-delay {DelayMilliseconds} ms", op, delay));
                    }

                    return Task.Delay(delay);
                });
            }

            prev.ContinueWith(_ => RunInContext(() =>
            {
                try
                {
                    if (timeout != Timeout.Infinite)
                    {
                        _logger.LogDebug("{Operation} starting with {TimeoutMilliseconds} ms timeout", op, timeout);

                        var cts = new CancellationTokenSource();
                        var timeoutTask = Task.Delay(timeout, cts.Token);

                        // when the timeout task completes without being canceled,
                        // notify the operation of the timeout
                        timeoutTask.ContinueWith(_ => RunInContext(() =>
                        {
                            try
                            {
                                if (!t.IsCompleted)
                                {
                                    op.OnTimeout();
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "{Operation} OnTimeout handler crashed", op);
                            }
                            if (!t.IsCompleted)
                            {
                                op.Abort(new TimeoutException());
                            }
                        }), default, TaskContinuationOptions.NotOnCanceled, _scheduler);

                        // get rid of the timeout task when the operation task completes
                        t.ContinueWith(_ =>
                        {
                            cts.Cancel();
                            cts.Dispose();
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    else
                    {
                        _logger.LogDebug("{Operation} starting without timeout", op);
                    }

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        t.ContinueWith(_ => RunInContext(() =>
                        {
                            if (t.IsCompletedSuccessfully)
                            {
                                _logger.LogDebug("{Operation} completed successfully with result {Result}", op, t.Result);
                            }
                            else if (t.IsCanceled)
                            {
                                _logger.LogDebug("{Operation} has been canceled", op);
                            }
                            else
                            {
                                _logger.LogDebug(t.Exception, "{Operation} has failed with an error", op);
                            }
                        }), TaskContinuationOptions.ExecuteSynchronously);
                    }

                    op.Start(this);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{Operation} crashed on start", op);
                    op.Abort(e);
                }
            }), _scheduler);

            return t;
        }

        public ILogger Logger => _logger;

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
