using System;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Represents allowed operations and properties of a Bluetooth LE Characteristic
    /// </summary>
    [Flags]
    public enum CharacteristicProperties
    {
        /// <summary>Characteristic value may be placed in advertisements</summary>
        Broadcast = 1,
        /// <summary>Characteristic can be read</summary>
        Read = 2,
        /// <summary>Characteristic can be written without response</summary>
        WriteWithoutResponse = 4,
        /// <summary>Characteristic can be written</summary>
        Write = 8,
        /// <summary>Characteristic value change notifications can be requested</summary>
        Notify = 0x10,
        /// <summary>Characteristic value change indication with confirmation can be requested</summary>
        Indicate = 0x20,
        /// <summary>Signed writes can be used</summary>
        WriteSigned = 0x40,
        /// <summary>Characteristic has extended properties</summary>
        Extended = 0x80,
    }
}
