using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace triaxis.Xamarin.BluetoothLE
{
    abstract class Operation<T> : IOperation<T>
    {
        TaskCompletionSource<T> _tcs = new TaskCompletionSource<T>();

        public Task<T> Task => _tcs.Task;
        Task IOperation.Task => Task;

        protected bool SetException(Exception e) => _tcs.TrySetException(e);
        protected bool SetException(string message) => SetException(new BluetoothLEException(message));
        protected bool SetResult(T res) => _tcs.TrySetResult(res);
        protected bool SetCanceled() => _tcs.TrySetCanceled();

        public virtual int StartDelay => 0;
        public abstract CancellationTokenRegistration Start(CancellationToken cancellationToken);
        public void Abort(Exception e) => SetException(e);
        public void Cancel() => SetCanceled();

        public override string ToString()
        {
            return $"{GetType().Name}[{GetHashCode()}]";
        }
    }

    abstract class Operation : Operation<object>
    {
    }
}
