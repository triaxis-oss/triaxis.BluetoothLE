using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Represents a Bluetooth LE Peripheral device
    /// </summary>
    public interface IPeripheral
    {
        /// <summary>
        /// UUID of the device
        /// </summary>
        ref readonly Uuid Uuid { get; }

        /// <summary>
        /// Invalidates the services cached by the system
        /// </summary>
        /// <remarks>
        /// Call this when there is high likelihood of service/characteristic changes, such as after
        /// firmwar upgrade, etc.
        /// </remarks>
        void InvalidateServiceCache();
        /// <summary>
        /// Attempts to create a connection with the peripheral with optional timeout
        /// </summary>
        Task<IPeripheralConnection> ConnectAsync(int msTimeout = Timeout.Infinite);
        /// <summary>
        /// Attempts to create a connection with the peripheral, performing attempts in a pattern relative to a specific advertisement
        /// </summary>
        Task<IPeripheralConnection> ConnectPatternAsync(IAdvertisement reference, int msPeriod, int msBefore, int msAfter, int attempts);
    }
}
