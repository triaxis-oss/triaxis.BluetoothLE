using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Represent an exception that occured while scanning for Bluetooth LE Peripherals
    /// </summary>
    public class ScanException : BluetoothLEException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="ScanException" />
        /// </summary>
        public ScanException(ScanFailure error)
            : base(error.ToString())
        {
        }
    }
}
