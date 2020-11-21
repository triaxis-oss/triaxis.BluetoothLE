using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    class Advertisement : IAdvertisement
    {
        readonly Peripheral _peripheral;
        readonly int _rssi;
        readonly int _txPower;
        readonly byte[] _localName;
        readonly byte[] _manufacturerData;
        readonly Guid[] _services;
        
        public Advertisement(Peripheral device, NSDictionary advertisementData, NSNumber rssi)
        {
            _peripheral = device;
            _rssi = rssi.Int32Value;
            if (advertisementData[CBAdvertisement.DataLocalNameKey] is NSString name)
                _localName = Encoding.UTF8.GetBytes(name);
            if (advertisementData[CBAdvertisement.DataManufacturerDataKey] is NSData data)
                _manufacturerData = data.ToArray();
            if (advertisementData[CBAdvertisement.DataTxPowerLevelKey] is NSNumber number)
                _txPower = number.Int32Value;
            if (advertisementData[CBAdvertisement.DataServiceUUIDsKey] is NSArray services)
                _services = ExtractGuids(services);
        }

        private Guid[] ExtractGuids(NSArray array)
        {
            var res = new Guid[array.Count];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = array.GetItem<CBUUID>((nuint)i).ToGuid();
            }
            return res;
        }

        public byte[] this[AdvertisementRecord record]
        {
            get
            {
                switch (record)
                {
                    case AdvertisementRecord.CompleteLocalName:
                        return _localName;
                    case AdvertisementRecord.ManufacturerData:
                        return _manufacturerData;
                    default:
                        return null;
                }
            }
        }

        public IPeripheral Peripheral => _peripheral;

        public int Rssi => _rssi;
        public int TxPower => _txPower;
        public Guid[] Services => _services;
    }
}
