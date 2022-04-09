using System;
using System.Collections.Generic;
using System.Text;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Determines the state of the Bluetooth LE adapter
    /// </summary>
    public enum AdapterState
    {
        /// <summary>Unknown state</summary>
        Unknown,
        /// <summary>The adapter is turned on and ready</summary>
        On,
        /// <summary>The adapter is turned off</summary>
        Off,
        /// <summary>The device does not support Bluetooth LE</summary>
        Unsupported,
        /// <summary>The application is not authorized to use Bluetooth LE</summary>
        Unauthorized,
        /// <summary>The adapter is transitioning to a new state</summary>
        Transitioning,
    }

    /// <summary>
    /// Represents a Bluetooth LE Adapter
    /// </summary>
    public interface IAdapter
    {
        /// <summary>
        /// Gets the current adapter state
        /// </summary>
        AdapterState State { get; }

        /// <summary>
        /// Gets an observable for scanning for peripherals in range
        /// </summary>
        IObservable<IAdvertisement> Scan();

        /// <summary>
        /// Gets an observable for scanning for peripherals in range, advertising the specified services
        /// </summary>
        IObservable<IAdvertisement> Scan(params ServiceUuid[] services);
    }
}
