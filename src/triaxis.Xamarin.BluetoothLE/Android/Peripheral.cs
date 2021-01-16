using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace triaxis.Xamarin.BluetoothLE.Android
{
    class Peripheral : IPeripheral
    {
        Adapter _adapter;
        readonly Uuid _uuid;
        BluetoothDevice _device;
        HashSet<PeripheralConnection> _connections = new HashSet<PeripheralConnection>();
        int _invalidateCache;

        public Peripheral(Adapter adapter, Uuid uuid, BluetoothDevice device)
        {
            _adapter = adapter;
            _uuid = uuid;
            _device = device;
        }

        public Adapter Adapter => _adapter;
        public ref readonly Uuid Uuid => ref _uuid;
        public byte[] HardwareAddress
        {
            get => _uuid.ToByteArrayLE().Take(6).ToArray();
            set { }
        }
        public BluetoothDevice Device => _device;
        public bool IsConnected { get; private set; }

        internal void UpdateDevice(BluetoothDevice device)
        {
            _device = device;
        }

        internal void AddConnection(PeripheralConnection connection)
        {
            lock (_connections)
            {
                _connections.Add(connection);
                IsConnected = true;
            }
        }

        internal void RemoveConnection(PeripheralConnection connection)
        {
            lock (_connections)
            {
                _connections.Remove(connection);
                if (IsConnected && _connections.Count == 0)
                {
                    IsConnected = false;
                    _adapter.Reschedule();
                }
            }
        }

        public void InvalidateServiceCache()
            => _invalidateCache = 1;
        internal bool CacheInvalidationRequested()
            => Interlocked.Exchange(ref _invalidateCache, 0) != 0;

        public Task<IPeripheralConnection> ConnectAsync(int timeout)
            => ConnectPatternAsync(null, 0, timeout == Timeout.Infinite ? 60000 : timeout, 0, 1);
        public Task<IPeripheralConnection> ConnectPatternAsync(IAdvertisement reference, int period, int before, int after, int attempts)
            => new PeripheralConnection(this).ConnectAsync((Advertisement)reference, period, before, after, attempts);

        public override string ToString()
            => $"{_device.Name ?? "Device"} ({_device.Address})";
    }
}
