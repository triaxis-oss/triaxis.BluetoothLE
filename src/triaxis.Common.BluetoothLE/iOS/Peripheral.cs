using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE.iOS
#else
namespace triaxis.Maui.BluetoothLE.iOS
#endif
{
    class Peripheral : IPeripheral
    {
        private readonly Adapter _adapter;
        private readonly Uuid _uuid;
        private readonly ILogger _logger;
        private CBPeripheral _peripheral;
        private PeripheralConnection _connection;
        private int _connNum;

        public Peripheral(Adapter adapter, Uuid uuid, CBPeripheral peripheral)
        {
            _logger = adapter._loggerFactory.CreateLogger($"BLEPeripheral:{uuid}");
            _adapter = adapter;
            _uuid = uuid;
            _peripheral = peripheral;
        }

        internal void UpdateCBPeripheral(CBPeripheral peripheral)
        {
            _peripheral = peripheral;
        }

        public Adapter Adapter => _adapter;
        public ref readonly Uuid Uuid => ref _uuid;
        public CBPeripheral CBPeripheral => _peripheral;

        public void InvalidateServiceCache() { }    // not available on iOS

        public Task<IPeripheralConnection> ConnectAsync(int timeout)
        {
            if (_connection != null && _connection.Peripheral == _peripheral)
            {
                _logger.LogWarning("Reusing previous connection");
            }
            else
            {
                _connection = new PeripheralConnection(this, ++_connNum);
            }

            return new PeripheralConnectionInstance(_connection).ConnectAsync(timeout);
        }

        public Task<IPeripheralConnection> ConnectPatternAsync(IAdvertisement reference, int period, int before, int after, int attempts)
        {
            // pattern connect has no benefit on iOS,
            // the system is able to scan and connect to multiple devices in parallel
            return ConnectAsync(before + period * attempts + after);
        }

        internal void Connected(CBPeripheral peripheral)
        {
            _connection?.Connected(peripheral);
        }

        internal void ConnectionFailed(CBPeripheral peripheral, NSError error)
        {
            Interlocked.Exchange(ref _connection, null)?.ConnectionFailed(peripheral, error);
        }

        internal void Disconnected(CBPeripheral peripheral, NSError error)
        {
            Interlocked.Exchange(ref _connection, null)?.Disconnected(peripheral, error);
        }
    }
}
