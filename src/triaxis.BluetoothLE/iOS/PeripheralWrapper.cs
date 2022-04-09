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

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Represents a peripheral device with a specific UUID. Tracks the current
    /// <see cref="Peripheral" /> instance corresponding to a concrete
    /// <see cref="CBPeripheral" /> instance.
    /// </summary>
    class PeripheralWrapper : IPeripheral
    {
        private readonly Adapter _adapter;
        private readonly Uuid _uuid;
        private Peripheral _peripheral;
        private int _instNum;

        public PeripheralWrapper(Adapter adapter, Uuid uuid)
        {
            _adapter = adapter;
            _uuid = uuid;
        }

        internal Peripheral CreatePeripheral(CBPeripheral peripheral)
        {
            var p = new Peripheral(this, peripheral, ++_instNum);
            Interlocked.Exchange(ref _peripheral, p)?.OnPeripheralLost();
            return p;
        }

        public Adapter Adapter => _adapter;
        public ref readonly Uuid Uuid => ref _uuid;

        public void InvalidateServiceCache() { }    // not available on iOS

        public Task<IPeripheralConnection> ConnectAsync(int timeout)
            => _peripheral.ConnectAsync(timeout);

        public Task<IPeripheralConnection> ConnectPatternAsync(IAdvertisement reference, int period, int before, int after, int attempts)
        {
            // pattern connect has no benefit on iOS,
            // the system is able to scan and connect to multiple devices in parallel
            return ConnectAsync(before + period * attempts + after);
        }
    }
}
