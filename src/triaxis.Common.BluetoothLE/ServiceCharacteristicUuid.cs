using System;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE
#else
namespace triaxis.Maui.BluetoothLE
#endif
{
    /// <summary>
    /// Represents an UUID of a Bluetooth LE Characteristic combined with the UUID of the service to which it belongs
    /// </summary>
    public readonly struct ServiceCharacteristicUuid
    {
        private readonly ServiceUuid _service;
        private readonly CharacteristicUuid _characteristic;

        /// <summary>
        /// Common well-known characteristics for the Generic Access service
        /// </summary>
        public static class GenericAccess
        {
            /// <summary>
            /// The Device Name characteristic
            /// </summary>
            public static ServiceCharacteristicUuid DeviceName =>
                ServiceUuid.GenericAccess + new CharacteristicUuid(0x2A00);
            /// <summary>
            /// The Device Appearance characteristic
            /// </summary>
            public static readonly ServiceCharacteristicUuid Appearance =
                ServiceUuid.GenericAccess + new CharacteristicUuid(0x2A01);
        }

        /// <summary>
        /// Common well-known characteristics for the Device Information service
        /// </summary>
        public static class DeviceInformation
        {
            /// <summary>
            /// The System Id characteristic
            /// </summary>
            public static readonly ServiceCharacteristicUuid SystemId =
                ServiceUuid.DeviceInformation + new CharacteristicUuid(0x2A23);
            /// <summary>
            /// The Manufacturer Name characteristic
            /// </summary>
            public static readonly ServiceCharacteristicUuid ManufacturerName =
                ServiceUuid.DeviceInformation + new CharacteristicUuid(0x2A29);
        }

        /// <summary>
        /// Initializes a <see cref="ServiceCharacteristicUuid"/> from the specified <see cref="ServiceUuid"/> and <see cref="CharacteristicUuid"/>
        /// </summary>
        public ServiceCharacteristicUuid(in ServiceUuid service, in CharacteristicUuid characteristic)
        {
            _service = service;
            _characteristic = characteristic;
        }

        /// <summary>
        /// Gets the <see cref="ServiceUuid"/> of the service which defines this characteristic
        /// </summary>
        public ServiceUuid Service => _service;
        /// <summary>
        /// Gets the <see cref="CharacteristicUuid"/> of this characteristic
        /// </summary>
        public CharacteristicUuid Characteristic => _characteristic;
    }
}
