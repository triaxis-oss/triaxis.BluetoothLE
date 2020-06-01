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
    }
}
