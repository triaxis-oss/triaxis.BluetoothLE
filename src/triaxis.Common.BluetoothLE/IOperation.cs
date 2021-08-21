using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE
#else
namespace triaxis.Maui.BluetoothLE
#endif
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
