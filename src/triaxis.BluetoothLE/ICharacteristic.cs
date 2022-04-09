using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Represents a Bluetooth LE GATT Characteristic
    /// </summary>
    public interface ICharacteristic
    {
        /// <summary>
        /// UUID of the characteristic
        /// </summary>
        ref readonly CharacteristicUuid Uuid { get; }
        /// <summary>
        /// Additional characteristic properties
        /// </summary>
        CharacteristicProperties Properties { get; }

        /// <summary>
        /// Reads the characteristic value
        /// </summary>
        Task<byte[]> ReadAsync();
        /// <summary>
        /// Writes the characteristic value, returns only after the device has accepted the new value
        /// </summary>
        Task WriteAsync(params byte[] data);
        /// <summary>
        /// Writes the characteristic value, waits for the value to 
        /// </summary>
        Task WriteWithoutResponseAsync(params byte[] data);
        /// <summary>
        /// Returns an <see cref="IObservable{T}"/> that can be used to subscribe
        /// to characteristic value change notifications
        /// </summary>
        IObservable<byte[]> Notifications(); 
    }
}
