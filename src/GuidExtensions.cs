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
        /// Converts a big-endian sequence of bytes to a <see cerf="Guid" />
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
        /// Converts an integer to full Bluetooth LE <see cref="Guid" />
        /// </summary>
        /// <returns></returns>
        public static Guid ToBluetoothGuid(this int bleShortUuid)
            => new Guid(bleShortUuid, 0, 0x1000, s_uuidBleBase2);
    }
}
