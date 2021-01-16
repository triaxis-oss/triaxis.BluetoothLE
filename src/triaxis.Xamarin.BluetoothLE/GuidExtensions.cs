using System;
using System.Collections.Generic;
using System.Text;

namespace triaxis.Xamarin.BluetoothLE
{
    /// <summary>
    /// Bluetooth LE specific exensions to <see cref="Guid" />
    /// </summary>
    public static class GuidExtensions
    {
        static readonly byte[] s_uuidBleBase2 = { 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB };

        /// <summary>
        /// Converts a big-endian sequence of bytes to a <see cref="Guid" />
        /// </summary>
        public static Guid ToGuidBE(this byte[] data)
        {
            return new Guid(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3],
                (short)(data[4] << 8 | data[5]),
                (short)(data[6] << 8 | data[7]),
                data[8], data[9], data[10], data[11],
                data[12], data[13], data[14], data[15]);
        }

        /// <summary>
        /// Converts a <see cref="Guid" /> to a big-endian sequence of bytes
        /// </summary>
        public static byte[] ToBytesBE(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, 4);
                Array.Reverse(bytes, 4, 2);
                Array.Reverse(bytes, 6, 2);
            }
            return bytes;
        }

        /// <summary>
        /// Converts an integer to full Bluetooth LE <see cref="Guid" />
        /// </summary>
        /// <returns></returns>
        public static Guid ToBluetoothGuid(this int bleShortUuid)
            => new Guid(bleShortUuid, 0, 0x1000, s_uuidBleBase2);

        /// <summary>
        /// Converts a little-endian sequence of bytes to a <see cref="Guid" />
        /// </summary>
        /// <returns></returns>
        public static Guid ToGuidLE(this byte[] array, int offset = 0, int length = 16)
        {
            switch (length)
            {
                case 2:
                    return (array[offset] | array[offset + 1] << 8).ToBluetoothGuid();
                case 4:
                    return (array[offset] | array[offset + 1] << 8 | array[offset + 2] | array[offset + 3]).ToBluetoothGuid();
                case 16:
                    return new Guid(array[offset + 12] | array[offset + 13] << 8 | array[offset + 14] << 16 | array[offset + 15] << 24,
                        (short)(array[offset + 10] | array[offset + 11] << 8),
                        (short)(array[offset + 8] | array[offset + 9] << 8),
                        array[offset + 7], array[offset + 6], array[offset + 5], array[offset + 4],
                        array[offset + 3], array[offset + 2], array[offset + 1], array[offset]
                        );
                default:
                    throw new ArgumentException("Unsupported length", "length");
            }
        }
    }
}
