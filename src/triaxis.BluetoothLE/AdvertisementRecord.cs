﻿using System;
using System.Collections.Generic;
using System.Text;

namespace triaxis.BluetoothLE
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
