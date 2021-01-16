using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace triaxis.Xamarin.BluetoothLE.Android
{
    class Advertisement : IAdvertisement
    {
        Peripheral _peripheral;
        int _rssi;
        int _txPower;
        byte[] _data;
        int _time;
        ServiceUuid[] _services;

        public Advertisement(Peripheral peripheral, int rssi, int txPower, byte[] data)
        {
            _peripheral = peripheral;
            _rssi = rssi;
            _txPower = txPower;
            _data = data;
            _time = System.Environment.TickCount;
        }

        public byte[] this[AdvertisementRecord record]
        {
            get
            {
                int i = 0;
                while (i < _data.Length)
                {
                    int len = _data[i];
                    if (len == 0 || i + len >= _data.Length)
                        break;
                    if ((AdvertisementRecord)_data[i + 1] == record)
                    {
                        byte[] res = new byte[len - 1];
                        Array.Copy(_data, i + 2, res, 0, len - 1);
                        return res;
                    }
                    i += 1 + len;
                }
                return null;
            }
        }

        public IPeripheral Peripheral => _peripheral;
        public int Rssi => _rssi;
        public int TxPower => _txPower;
        public int Time => _time;
        public ServiceUuid[] Services => _services ??= ExtractServices();

        private ServiceUuid[] ExtractServices()
        {
            int i = 0;
            List<ServiceUuid> res = null;
            while (i < _data.Length)
            {
                int len = _data[i];
                if (len == 0 || i + len >= _data.Length)
                    break;
                int uuidLen = 0;
                switch (_data[i + 1])
                {
                    case 2: case 3: // complete/incomplete 16-bit uids
                        uuidLen = 2;
                        break;
                    case 4: case 5: // complete/incomplete 32-bit uuids
                        uuidLen = 4;
                        break;
                    case 6: case 7: // complete/incomplete 128-bit uuids
                        uuidLen = 16;
                        break;
                }
                int n = i + 2;
                i += 1 + len;
                if (uuidLen > 0)
                {
                    for (; n + uuidLen <= i; n += uuidLen)
                    {
                        (res ??= new List<ServiceUuid>()).Add(new ServiceUuid(Uuid.FromLE(_data.AsSpan(n, uuidLen))));
                    }
                }
            }
            return res?.ToArray() ?? Array.Empty<ServiceUuid>();
        }
    }
}
