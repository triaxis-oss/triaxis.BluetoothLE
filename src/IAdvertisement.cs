using System;
using System.Collections.Generic;
using System.Text;

namespace triaxis.Xamarin.BluetoothLE
{
    /// <summary>
    /// Represent a Bluetooth LE Advertisement received from a peripheral
    /// </summary>
    public interface IAdvertisement
    {
        /// <summary>
        /// The <see cref="IPeripheral" /> from which the advertisement has been received
        /// </summary>
        IPeripheral Peripheral { get; }

        /// <summary>
        /// Received signal strength indication of the advertisement
        /// </summary>
        int Rssi { get; }

        /// <summary>
        /// Transmitter power of the peripheral
        /// </summary>
        int TxPower { get; }

        /// <summary>
        /// Retrieves the specified data from the advertisement
        /// </summary>
        byte[] this[AdvertisementRecord record] { get; }
    }
}
