using System;
using System.Collections.Generic;
using System.Linq;
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
    class Advertisement : IAdvertisement
    {
        private readonly IPeripheral _peripheral;
        private readonly int _rssi;
        private readonly int _txPower;
        private readonly DateTime _timestamp;
        private readonly byte[] _localName;
        private readonly byte[] _manufacturerData;
        private readonly ServiceUuid[] _services;
        
        public Advertisement(IPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
        {
            _peripheral = peripheral;
            _rssi = rssi.Int32Value;
            if (advertisementData[CBAdvertisement.DataLocalNameKey] is NSString name)
                _localName = Encoding.UTF8.GetBytes(name);
            if (advertisementData[CBAdvertisement.DataManufacturerDataKey] is NSData data)
                _manufacturerData = data.ToArray();
            if (advertisementData[CBAdvertisement.DataTxPowerLevelKey] is NSNumber number)
                _txPower = number.Int32Value;
            if (advertisementData[CBAdvertisement.DataServiceUUIDsKey] is NSArray services)
                _services = ExtractServiceUuids(services);
            if (advertisementData["kCBAdvDataTimestamp"] is NSNumber stamp)
                _timestamp = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(stamp.DoubleValue);
            else
                _timestamp = DateTime.UtcNow;
        }

        private ServiceUuid[] ExtractServiceUuids(NSArray array)
        {
            var res = new ServiceUuid[array.Count];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = array.GetItem<CBUUID>((nuint)i).ToUuid();
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
        public DateTime Timestamp => _timestamp;
        public ServiceUuid[] Services => _services;
    }
}
