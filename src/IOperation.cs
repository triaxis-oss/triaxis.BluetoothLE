using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace triaxis.Xamarin.BluetoothLE
{
    /// <summary>
    /// Internal representation of a Bluetooth LE operation
    /// </summary>
    interface IOperation
    {
        Task Task { get; }
        int StartDelay { get; }
        CancellationTokenRegistration Start(CancellationToken cancellationToken);
        void Abort(Exception e);
        void Cancel();
    }

    interface IOperation<T> : IOperation
    {
        new Task<T> Task { get; }
    }
}
