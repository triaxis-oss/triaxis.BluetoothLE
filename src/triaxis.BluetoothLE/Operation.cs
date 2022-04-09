using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace triaxis.BluetoothLE
{
    class OperationBase
    {
        private static int s_id;

        public OperationBase()
        {
            LogScope = new() { ["Operation"] = this, ["OperationId"] = OperationId };
        }

        public int OperationId { get; } = Interlocked.Increment(ref s_id);
        protected Dictionary<string, object> LogScope { get; }

        public override string ToString()
        {
            return $"{GetType().Name}[{OperationId}]";
        }
    }

    abstract class Operation<T> : OperationBase, IOperation<T>
    {
        private readonly TaskCompletionSource<T> _tcs = new TaskCompletionSource<T>();
        private OperationQueue _q;

        public Task<T> Task => _tcs.Task;
        Task IOperation.Task => Task;

        protected ILogger Logger => _q?.Logger;

        protected bool SetException(Exception e) => _tcs.TrySetException(e);
        protected bool SetException(string message) => SetException(new BluetoothLEException(message));
        protected bool SetResult(T res) => _tcs.TrySetResult(res);
        protected bool SetCanceled() => _tcs.TrySetCanceled();

        protected virtual int StartDelay => 0;
        protected abstract void Start();
        protected virtual void OnTimeout() { }

        int IOperation.StartDelay => StartDelay;
        void IOperation.Start(OperationQueue q)
        {
            _q = q;
            Start();
        }

        void IOperation.OnTimeout() => OnTimeout();
        IEnumerable<KeyValuePair<string, object>> IOperation.GetScope() => LogScope;

        void IOperation.Abort(Exception e) => SetException(e);
        void IOperation.Cancel() => SetCanceled();
    }

    abstract class Operation : Operation<object>
    {
    }
}
