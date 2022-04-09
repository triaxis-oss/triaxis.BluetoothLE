using System;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Represents an UUID of a Bluetooth LE Service
    /// </summary>
    public readonly struct ServiceUuid : IComparable<ServiceUuid>, IEquatable<ServiceUuid>
    {
        /// <summary>
        /// Gets the <see cref="ServiceUuid"/> of the Generic Access service
        /// </summary>
        public static readonly ServiceUuid GenericAccess = new ServiceUuid(0x1800);
        /// <summary>
        /// Gets the <see cref="ServiceUuid"/> of the Device Information service
        /// </summary>
        public static readonly ServiceUuid DeviceInformation = new ServiceUuid(0x180A);

        private readonly Uuid _uuid;

        /// <summary>
        /// Initializes a <see cref="ServiceUuid"/> from a <see cref="Uuid"/>
        /// </summary>
        public ServiceUuid(in Uuid uuid)
        {
            _uuid = uuid;
        }

        /// <summary>
        /// Initializes a <see cref="ServiceUuid"/> from a short Bluetooth LE UUID
        /// </summary>
        public ServiceUuid(uint bleShortUuid)
        {
            _uuid = new Uuid(bleShortUuid);
        }

        /// <summary>
        /// Initializes a <see cref="ServiceUuid"/> from UUID segments
        /// </summary>
        public ServiceUuid(uint a, ushort b, ushort c, ushort d, ulong e)
        {
            _uuid = new Uuid(a, b, c, d, e);
        }

        /// <summary>
        /// Initializes a <see cref="ServiceUuid"/> from UUID halves
        /// </summary>
        public ServiceUuid(ulong a, ulong b)
        {
            _uuid = new Uuid(a, b);
        }

        /// <summary>
        /// Converts an <see cref="Uuid"/> to a <see cref="ServiceUuid"/>
        /// </summary>
        public static implicit operator ServiceUuid(in Uuid uuid)
            => new ServiceUuid(uuid);

        /// <summary>
        /// Gets the <see cref="Uuid"/> which represents this <see cref="ServiceUuid"/>
        /// </summary>
        public Uuid Uuid => _uuid;

        /// <summary>
        /// Compares the <see cref="ServiceUuid"/> to another <see cref="ServiceUuid"/>
        /// </summary>
        public int CompareTo(ServiceUuid other)
            => _uuid.CompareTo(other._uuid);

        /// <summary>
        /// Checks if the <see cref="ServiceUuid"/> is the same as another <see cref="ServiceUuid"/>
        /// </summary>
        public bool Equals(ServiceUuid other)
            => _uuid.Equals(other._uuid);

        /// <summary>
        /// Checks if the <see cref="CharacteristicUuid"/> is the same as another <see cref="ServiceUuid"/>
        /// </summary>
        public override bool Equals(object obj)
            => obj is ServiceUuid other && _uuid.Equals(other._uuid);

        /// <summary>
        /// Checks if the two <see cref="ServiceUuid"/>s are the same
        /// </summary>
        public static bool operator ==(in ServiceUuid u1, in ServiceUuid u2)
            => u1._uuid == u2._uuid;

        /// <summary>
        /// Checks if the two <see cref="ServiceUuid"/>s are different
        /// </summary>
        public static bool operator !=(in ServiceUuid u1, in ServiceUuid u2)
            => u1._uuid != u2._uuid;

        /// <summary>
        /// Gets the hash code of the <see cref="ServiceUuid"/>
        /// </summary>
        public override int GetHashCode()
            => _uuid.GetHashCode();

        /// <summary>
        /// Gets the string representation of the <see cref="ServiceUuid"/>
        /// </summary>
        public override string ToString()
            => _uuid.ToString();

        /// <summary>
        /// Combines the specified <see cref="ServiceUuid"/> with the specified <see cref="CharacteristicUuid"/>
        /// to create a <see cref="ServiceCharacteristicUuid"/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="characteristic"></param>
        /// <returns></returns>
        public static ServiceCharacteristicUuid operator +(in ServiceUuid service, in CharacteristicUuid characteristic)
            => new ServiceCharacteristicUuid(service, characteristic);
    }
}
