using System;
using System.Runtime.InteropServices;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE
#else
namespace triaxis.Maui.BluetoothLE
#endif
{
    /// <summary>
    /// Represents a 128-bit Universally Unique ID
    /// </summary>
    public readonly struct Uuid : IComparable<Uuid>, IEquatable<Uuid>
    {
        const ulong _uuidBleBase2 = 0x800000805F9B34FB;

        /// <summary>
        /// Internal representation is in "visual" order regardless of platform byte ordering,
        /// i.e. a = left 8 octets, b = right 8 octets
        /// </summary>
        private readonly ulong a, b;

        /// <summary>
        /// Initializes a new <see cref="Uuid"/> from a set of integers taken from the UUID segments
        /// </summary>
        public Uuid(uint a, ushort b, ushort c, ushort d, ulong e)
        {
            this.a = (ulong)a << 32 | (uint)b << 16 | c;
            this.b = (ulong)d << 48 | e;
        }

        /// <summary>
        /// Initializes a new <see cref="Uuid"/> from two 64-bit integeres
        /// </summary>
        public Uuid(ulong a, ulong b)
        {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        /// Initializes a new <see cref="Uuid"/> from a short form Bluetooth LE UUID
        /// </summary>
        /// <returns></returns>
        public Uuid(uint bleShortUuid)
        {
            a = (ulong)bleShortUuid << 32 | 0x1000;
            b = _uuidBleBase2;
        }

        /// <summary>
        /// Initializes a new <see cref="Uuid"/> structure from the specified string
        /// </summary>
        /// <remarks>
        /// First 32 hexadecimal digits found in the string are taken,
        /// all other characters are ignored
        /// </remarks>
        /// <param name="value">UUID string value</param>
        public Uuid(string value)
        {
            int n = 0;
            a = b = 0;
            foreach (char c in value)
            {
                int d;
                if (c >= '0' && c <= '9')
                    d = c - '0';
                else if (c >= 'a' && c <= 'f')
                    d = c - 'a' + 10;
                else if (c >= 'A' && c <= 'F')
                    d = c - 'A' + 10;
                else
                    continue;
                if (n < 16)
                    a |= (ulong)d << (15 - n) * 4;
                else
                    b |= (ulong)d << (31 - n) * 4;
                if (++n == 32)
                    break;
            }
        }

        /// <summary>
        /// Gets the left 64-bits of the <see cref="Uuid" />
        /// </summary>
        public ulong LeftHalf => a;
        /// <summary>
        /// Gets the right 64-bits of the <see cref="Uuid" />
        /// </summary>
        public ulong RightHalf => b;
        
        /// <summary>
        /// Checks if the <see cref="Uuid" /> is a Bluetooth LE UUID
        /// </summary>
        public bool IsBluetoothLE => b == _uuidBleBase2 && (uint)a == 0x1000;

        /// <summary>
        /// Creates a <see cref="Uuid"/> from a little-endian sequence of bytes
        /// </summary>
        /// <returns></returns>
        public static Uuid FromLE(ReadOnlySpan<byte> data)
        {
            switch (data.Length)
            {
                case 2:
                    return new Uuid((uint)(data[0] | data[1] << 8));
                case 4:
                    return new Uuid((uint)(data[0] | data[1] << 8 | data[2] << 16 | data[3] << 24));
                case 16:
                    var longs = MemoryMarshal.Cast<byte, ulong>(data);
                    if (BitConverter.IsLittleEndian)
                    {
                        return new Uuid(longs[1], longs[0]);
                    }
                    else
                    {
                        return new Uuid(Reverse(longs[1]), Reverse(longs[0]));
                    }
                default:
                    throw new ArgumentException("Unsupported length", "length");
            }
        }

        /// <summary>
        /// Creates an <see cref="Uuid"/> from a big-endian sequence of bytes
        /// </summary>
        public static Uuid FromBE(ReadOnlySpan<byte> data)
        {
            switch (data.Length)
            {
                case 2:
                    return new Uuid((uint)(data[0] << 8 | data[1]));
                case 4:
                    return new Uuid((uint)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]));
                case 16:
                    var longs = MemoryMarshal.Cast<byte, ulong>(data);
                    if (BitConverter.IsLittleEndian)
                    {
                        return new Uuid(Reverse(longs[0]), Reverse(longs[1]));
                    }
                    else
                    {
                        return new Uuid(longs[0], longs[1]);
                    }
                default:
                    throw new ArgumentException("Unsupported length", "length");
            }
        }

        /// <summary>
        /// Converts the <see cref="Uuid" /> to a big-endian sequence of bytes
        /// </summary>
        public byte[] ToByteArrayBE()
        {
            Span<ulong> data = stackalloc ulong[2];

            if (BitConverter.IsLittleEndian)
            {
                data[0] = Reverse(a); data[1] = Reverse(b);
            }
            else
            {
                data[0] = a; data[1] = b;
            }

            return MemoryMarshal.AsBytes(data).ToArray();
        }


        /// <summary>
        /// Converts the <see cref="Uuid" /> to a little-endian sequence of bytes
        /// </summary>
        public byte[] ToByteArrayLE()
        {
            Span<ulong> data = stackalloc ulong[2];

            if (BitConverter.IsLittleEndian)
            {
                data[1] = a; data[0] = b;
            }
            else
            {
                data[1] = Reverse(a); data[0] = Reverse(b);
            }

            return MemoryMarshal.AsBytes(data).ToArray();
        }

        /// <summary>
        /// Compares the <see cref="ServiceUuid"/> to another <see cref="ServiceUuid"/>
        /// </summary>
        public int CompareTo(Uuid other)
            => a < other.a ? -1 : a > other.a ? 1 : b < other.b ? -1 : b > other.b ? 1 : 0;

        /// <summary>
        /// Checks if the <see cref="ServiceUuid"/> is the same as another <see cref="ServiceUuid"/>
        /// </summary>
        public bool Equals(in Uuid other)
            => a == other.a && b == other.b;

        /// <summary>
        /// Checks if the <see cref="ServiceUuid"/> is the same as another <see cref="ServiceUuid"/>
        /// </summary>
        public bool Equals(Uuid other)
            => a == other.a && b == other.b;

        /// <summary>
        /// Checks if the <see cref="ServiceUuid"/> is the same as another <see cref="ServiceUuid"/>
        /// </summary>
        public override bool Equals(object obj)
            => obj is Uuid other && a == other.a && b == other.b;

        /// <summary>
        /// Checks if the two <see cref="ServiceUuid"/>s are the same
        /// </summary>
        public static bool operator ==(in Uuid u1, in Uuid u2)
            => u1.a == u2.a && u1.b == u2.b;

        /// <summary>
        /// Checks if the two <see cref="ServiceUuid"/>s are different
        /// </summary>
        public static bool operator !=(in Uuid u1, in Uuid u2)
            => u1.a != u2.a || u1.b != u2.b;

        /// <summary>
        /// Gets the hash code of the <see cref="ServiceUuid"/>
        /// </summary>
        public override int GetHashCode()
            => (int)a ^ (int)(a >> 32) ^ (int)b ^ (int)(b >> 32);

        /// <summary>
        /// Gets the string representation of the <see cref="ServiceUuid"/>
        /// </summary>
        public override string ToString()
            => IsBluetoothLE ? $"BLE:{a >> 32:X}" : $"{a >> 32:X8}-{(a >> 16) & 0xFFFF:X4}-{a & 0xFFFF:X4}-{b >> 48:X4}-{b << 16 >> 16:X12}";

        /// <summary>
        /// Reverses the byte order of a 64-bit integer
        /// </summary>
        private static ulong Reverse(ulong l)
        {
            // ABCDEFGH => BADCFEHG
            l = ((l & 0xFF00FF00FF00FF00u) >> 8) | ((l & 0x00FF00FF00FF00FFu) << 8);
            // BADCFEHG => DCBAHGFE
            l = ((l & 0xFFFF0000FFFF0000u) >> 16) | ((l & 0x0000FFFF0000FFFFu) << 16);
            // DCBAHGFE => HGFEDCBA
            return (l >> 32) | (l << 32);
        }
    }
}
