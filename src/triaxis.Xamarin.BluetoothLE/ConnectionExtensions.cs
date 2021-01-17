using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace triaxis.Xamarin.BluetoothLE
{
    /// <summary>
    /// Convenience exensions to <see cref="IPeripheralConnection" />
    /// </summary>
    public static class ConnectionExtension
    {
        /// <summary>
        /// Tries to find the specified characteristic of the specified service
        /// </summary>
        /// <returns>The <see cref="ICharacteristic" /> if found, <see langword="null" /> otherwise.</returns>
        public static async Task<ICharacteristic> FindServiceCharacteristicAsync(this IPeripheralConnection connection, ServiceUuid service, CharacteristicUuid characteristic)
        {
            var svc = (await connection.GetServicesAsync())?.FirstOrDefault(svc => svc.Uuid == service);
            if (svc == null)
            {
                return null;
            }

            return (await svc.GetCharacteristicsAsync())?.FirstOrDefault(ch => ch.Uuid == characteristic);
        }

        /// <summary>
        /// Tries to find the specified characteristic of the specified service
        /// </summary>
        /// <returns>The <see cref="ICharacteristic" /> if found, <see langword="null" /> otherwise.</returns>
        public static Task<ICharacteristic> FindServiceCharacteristicAsync(this IPeripheralConnection connection, in ServiceCharacteristicUuid characteristic)
            => connection.FindServiceCharacteristicAsync(characteristic.Service, characteristic.Characteristic);
    }
}
