using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CoreBluetooth;
using Foundation;
using UIKit;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE.iOS
#else
namespace triaxis.Maui.BluetoothLE.iOS
#endif
{
    static class PrivateExtensions
    {
        public static Uuid ToUuid(this CBUUID uuid)
        {
            var bytes = uuid.Data.ToArray();
            return Uuid.FromBE(bytes);
        }

        public static Uuid ToUuid(this NSUuid uuid)
        {
            var bytes = uuid.GetBytes();
            return Uuid.FromBE(bytes);
        }

        public static Exception ToException(this NSError error)
        {
            return error == null ? null : new NSErrorException(error);
        }
    }
}
