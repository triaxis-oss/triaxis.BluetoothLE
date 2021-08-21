using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE
#else
namespace triaxis.Maui.BluetoothLE
#endif
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
        public static async Task<ICharacteristic> FindServiceCharacteristicAsync(this IPeripheralConnection connection, ServiceUuid service, CharacteristicUuid characteristic, bool throwOnError = false)
        {
            var svc = (await connection.GetServicesAsync())?.FirstOrDefault(svc => svc.Uuid == service);
            if (svc == null)
            {
                if (throwOnError)
                {
                    throw new BluetoothLEException($"Service not found: {service}");
                }
                return null;
            }

            var ch = (await svc.GetCharacteristicsAsync())?.FirstOrDefault(ch => ch.Uuid == characteristic);
            if (ch == null)
            {
                if (throwOnError)
                {
                    throw new BluetoothLEException($"Characteristic not found: {characteristic} (service {service})");
                }
            }

            return ch;
        }

        /// <summary>
        /// Tries to find the specified characteristic of the specified service
        /// </summary>
        /// <returns>The <see cref="ICharacteristic" /> if found, <see langword="null" /> otherwise.</returns>
        public static Task<ICharacteristic> FindServiceCharacteristicAsync(this IPeripheralConnection connection, in ServiceCharacteristicUuid characteristic, bool throwOnError = false)
            => connection.FindServiceCharacteristicAsync(characteristic.Service, characteristic.Characteristic, throwOnError);
    }
}
