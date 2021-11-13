﻿using System;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE
#else
namespace triaxis.Maui.BluetoothLE
#endif
{
    /// <summary>
    /// An exception thrown by the Bluetooth LE stack
    /// </summary>
    public class BluetoothLEException : Exception
    {
        /// <summary>
        /// Creates a new instance of a <see cref="BluetoothLEException" />
        /// </summary>
        public BluetoothLEException(string message)
            : base(message)
        {
        }
    }
}