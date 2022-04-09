using System;
using System.Collections.Generic;
using System.Text;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Platform-specific Bluetooth LE implementation
    /// </summary>
    public interface IBluetoothLE
    {
        /// <summary>
        /// Gets an observable returning values when Bluetooth LE adapter availability changes
        /// </summary>
        IObservable<IAdapter> WhenAdapterChanges();
    }
}
