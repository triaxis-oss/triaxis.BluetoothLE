using System;
using System.Collections.Generic;
using System.Text;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE
#else
namespace triaxis.Maui.BluetoothLE
#endif
{
    /// <summary>
    /// Additional fields found in an <see cref="IAdvertisement" />
    /// </summary>
    public enum AdvertisementRecord : byte
    {
        /// <summary>Complete local name of the peripheral</summary>
        CompleteLocalName = 9,
        /// <summary>Custom manufacturer data</summary>
        ManufacturerData = 0xFF,
    }
}
