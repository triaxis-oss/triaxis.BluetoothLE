using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Represents a Bluetooth LE GATT Service
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// UUID of the service
        /// </summary>
        ref readonly ServiceUuid Uuid { get; }

        /// <summary>
        /// Retrieves all characteristics defined by the service
        /// </summary>
        Task<IList<ICharacteristic>> GetCharacteristicsAsync();
    }
}
