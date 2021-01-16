using System;

namespace triaxis.Xamarin.BluetoothLE
{
    /// <summary>
    /// Represents an UUID of a Bluetooth LE Characteristic
    /// </summary>
    public readonly struct CharacteristicUuid : IComparable<CharacteristicUuid>, IEquatable<CharacteristicUuid>
    {
        private readonly Uuid _uuid;

        /// <summary>
        /// Initializes a <see cref="CharacteristicUuid"/> from a <see cref="Uuid"/>
        /// </summary>
        public CharacteristicUuid(in Uuid uuid)
        {
            _uuid = uuid;
        }

        /// <summary>
        /// Initializes a <see cref="CharacteristicUuid"/> from a short Bluetooth LE UUID
        /// </summary>
        public CharacteristicUuid(uint bleShortUuid)
        {
            _uuid = new Uuid(bleShortUuid);
        }

        /// <summary>
        /// Initializes a <see cref="CharacteristicUuid"/> from UUID segments
        /// </summary>
        public CharacteristicUuid(uint a, ushort b, ushort c, ushort d, ulong e)
        {
            _uuid = new Uuid(a, b, c, d, e);
        }

        /// <summary>
        /// Initializes a <see cref="CharacteristicUuid"/> from UUID halves
        /// </summary>
        public CharacteristicUuid(ulong a, ulong b)
        {
            _uuid = new Uuid(a, b);
        }

        /// <summary>
        /// Converts an <see cref="Uuid"/> to a <see cref="CharacteristicUuid"/>
        /// </summary>
        public static implicit operator CharacteristicUuid(in Uuid uuid)
            => new CharacteristicUuid(uuid);

        /// <summary>
        /// Gets the <see cref="Uuid"/> which represents this <see cref="CharacteristicUuid"/>
        /// </summary>
        public readonly Uuid Uuid => _uuid;

        /// <summary>
        /// Compares the <see cref="CharacteristicUuid"/> to another <see cref="CharacteristicUuid"/>
        /// </summary>
        public int CompareTo(CharacteristicUuid other)
            => _uuid.CompareTo(other._uuid);

        /// <summary>
        /// Checks if the <see cref="CharacteristicUuid"/> is the same as another <see cref="CharacteristicUuid"/>
        /// </summary>
        public bool Equals(CharacteristicUuid other)
            => _uuid.Equals(other._uuid);

        /// <summary>
        /// Checks if the <see cref="CharacteristicUuid"/> is the same as another <see cref="CharacteristicUuid"/>
        /// </summary>
        public override bool Equals(object obj)
            => obj is CharacteristicUuid other && _uuid.Equals(other._uuid);

        /// <summary>
        /// Checks if the two <see cref="CharacteristicUuid"/>s are the same
        /// </summary>
        public static bool operator ==(in CharacteristicUuid u1, in CharacteristicUuid u2)
            => u1._uuid == u2._uuid;

        /// <summary>
        /// Checks if the two <see cref="CharacteristicUuid"/>s are different
        /// </summary>
        public static bool operator !=(in CharacteristicUuid u1, in CharacteristicUuid u2)
            => u1._uuid != u2._uuid;

        /// <summary>
        /// Gets the hash code of the <see cref="CharacteristicUuid"/>
        /// </summary>
        public override int GetHashCode()
            => _uuid.GetHashCode();

        /// <summary>
        /// Gets the string representation of the <see cref="CharacteristicUuid"/>
        /// </summary>
        public override string ToString()
            => _uuid.ToString();
    }
}
