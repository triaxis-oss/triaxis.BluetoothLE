using System;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Extension methods for the <see cref="ICharacteristic" /> interface
    /// </summary>
    public static class CharacteristicExtensions
    {
        /// <summary>
        /// Determines if the characteristic can be read
        /// </summary>
        public static bool CanRead(this ICharacteristic characteristic)
            => (characteristic.Properties & CharacteristicProperties.Read) != 0;

        /// <summary>
        /// Determines if the characteristic can be written
        /// </summary>
        public static bool CanWrite(this ICharacteristic characteristic)
            => (characteristic.Properties & CharacteristicProperties.Write) != 0;

        /// <summary>
        /// Determines if the characteristic can be written without response
        /// </summary>
        public static bool CanWriteWithoutResponse(this ICharacteristic characteristic)
            => (characteristic.Properties & CharacteristicProperties.WriteWithoutResponse) != 0;

        /// <summary>
        /// Determines if the characteristic can provide value change notifications
        /// </summary>
        public static bool CanNotify(this ICharacteristic characteristic)
            => (characteristic.Properties & CharacteristicProperties.Notify) != 0;

        /// <summary>
        /// Determines if the characteristic can provide value change indications with confirmation
        /// </summary>
        public static bool CanIndicate(this ICharacteristic characteristic)
            => (characteristic.Properties & CharacteristicProperties.Indicate) != 0;
    }
}
