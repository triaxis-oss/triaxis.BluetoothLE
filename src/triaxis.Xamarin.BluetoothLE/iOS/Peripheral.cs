using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    class Peripheral : IPeripheral
    {
        readonly Adapter _adapter;
        readonly Guid _uuid;
        CBPeripheral _peripheral;
        List<PeripheralConnection> _connections = new List<PeripheralConnection>();

        public Peripheral(Adapter adapter, Guid uuid, CBPeripheral peripheral)
        {
            _adapter = adapter;
            _uuid = uuid;
            _peripheral = peripheral;
        }

        internal void UpdateCBPeripheral(CBPeripheral peripheral)
        {
            _peripheral = peripheral;
        }

        public Adapter Adapter => _adapter;
        public Guid Uuid => _uuid;
        public CBPeripheral CBPeripheral => _peripheral;

        public void InvalidateServiceCache() { }    // not available on iOS

        public Task<IPeripheralConnection> ConnectAsync(int timeout)
        {
            var con = new PeripheralConnection(this);
            _connections.Add(con);
            if (_connections.Count > 1)
                System.Diagnostics.Debug.WriteLine($"WARNING! {_connections.Count} parallel connections to the same device detected");
            return con.ConnectAsync(timeout);
        }

        public Task<IPeripheralConnection> ConnectPatternAsync(IAdvertisement reference, int period, int before, int after, int attempts)
        {
            // pattern connect has no benefit on iOS,
            // the system is able to scan and connect to multiple devices in parallel
            return ConnectAsync(before + period * attempts + after);
        }

        internal void Connected(CBPeripheral peripheral)
        {
            _connections.RemoveAll(con => !con.Connected(peripheral));
        }

        internal void ConnectionFailed(CBPeripheral peripheral, NSError error)
        {
            _connections.RemoveAll(con => con.ConnectionFailed(peripheral, error));
        }

        internal void Disconnected(CBPeripheral peripheral, NSError error)
        {
            _connections.RemoveAll(con => con.Disconnected(peripheral, error));
        }
    }
}
