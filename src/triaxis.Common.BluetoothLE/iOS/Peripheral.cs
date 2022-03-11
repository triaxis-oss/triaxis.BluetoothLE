using System;
using System.Threading.Tasks;
using CoreBluetooth;
using Microsoft.Extensions.Logging;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE.iOS
#else
namespace triaxis.Maui.BluetoothLE.iOS
#endif
{
    /// <summary>
    /// Wraps a single <see cref="CBPeripheral" /> instance, serializing
    /// all operations using an operation queue and handling its
    /// <see cref="CBPeripheralDelegate" /> callbacks. Also tracks all
    /// connections to the peripheral.
    /// </summary>
    partial class Peripheral
    {
        private readonly PeripheralWrapper _peripheral;
        private readonly CBPeripheral _cbPeripheral;
        private readonly OperationQueue _q;
        private readonly ILogger _logger;

        /// <summary>
        /// Currently active connection that can be safely reused for new connection instances.
        /// </summary>
        private Connection _connection;
        private int _connNum;

        public Peripheral(PeripheralWrapper peripheral, CBPeripheral cbPeripheral, int num)
        {
            _peripheral = peripheral;
            _cbPeripheral = cbPeripheral;
            cbPeripheral.Delegate = this;
            var loggerId = $"BLEPeripheral:{peripheral.Uuid}:{num}";
            _logger = peripheral.Adapter.CreateLogger(loggerId);
            _q = new OperationQueue(_logger);
        }

        public CBPeripheral CBPeripheral => _cbPeripheral;
        public PeripheralWrapper Wrapper => _peripheral;
        public Adapter Adapter => _peripheral.Adapter;
        public CBCentralManager CentralManager => Adapter.CentralManager;

        private Task<T> Enqueue<T>(Operation<T> op, int timeout = -1)
        {
            op.Bind(this);
            return _q.Enqueue(op, timeout);
        }

        internal Task<IPeripheralConnection> ConnectAsync(int timeout)
        {
            if (_connection is Connection con)
            {
                _logger.LogWarning("Reusing previous connection");
            }
            else
            {
                _connection = con = new Connection(this, ++_connNum);
            }

            return con.CreateInstance(timeout).ConnectTask;
        }

        private void CBConnect()
        {
            CentralManager.ConnectPeripheral(_cbPeripheral);
        }

        private void CBDisconnect()
        {
            CentralManager.CancelPeripheralConnection(_cbPeripheral);
        }

        /// <summary>
        /// Called when the OS-level CBPeripheral changes
        /// </summary>
        internal void OnPeripheralLost()
        {
            Enqueue(new PeripheralLostOperation());
        }
    }
}
