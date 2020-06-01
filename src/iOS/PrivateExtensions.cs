using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    static class PrivateExtensions
    {
        public static Guid ToGuid(this CBUUID uuid)
        {
            var bytes = uuid.Data.ToArray();
            if (bytes.Length == 2)
            {
                return ((bytes[0] << 8) | bytes[1]).ToBluetoothGuid();
            }
            else
            {
                return bytes.ToGuidBE();
            }
        }
    }
}
