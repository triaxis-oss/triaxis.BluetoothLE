using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    class Adapter : CBCentralManagerDelegate, IAdapter
    {
        Platform _owner;
        CBCentralManager _central;
        List<IObserver<IAdvertisement>> _scanners = new List<IObserver<IAdvertisement>>();
        Dictionary<Guid, Peripheral> _devices = new Dictionary<Guid, Peripheral>();

        public Adapter(Platform owner)
        {
            _owner = owner;
            _central = new CBCentralManager(this, null);
        }

        public AdapterState State
        {
            get
            {
                switch (_central.State)
                {
                    case CBCentralManagerState.PoweredOff: return AdapterState.Off;
                    case CBCentralManagerState.PoweredOn: return AdapterState.On;
                    case CBCentralManagerState.Resetting: return AdapterState.Transitioning;
                    case CBCentralManagerState.Unauthorized: return AdapterState.Unauthorized;
                    case CBCentralManagerState.Unsupported: return AdapterState.Unsupported;
                    default: return AdapterState.Unknown;
                }
            }
        }

        public CBCentralManager CentralManager => _central;

        public IObservable<IAdvertisement> Scan() => Observable.Create<IAdvertisement>(sub =>
        {
            _scanners.Add(sub);

            if (_scanners.Count == 1)
            {
                _central.ScanForPeripherals(null, new PeripheralScanningOptions
                {
                    AllowDuplicatesKey = true,
                });
            }

            return () =>
            {
                _scanners.Remove(sub);
                if (_scanners.Count == 0)
                    _central.StopScan();
            };
        });

        Peripheral GetPeripheral(CBPeripheral peripheral)
        {
            var uuid = new Guid(peripheral.Identifier.GetBytes());

            if (_devices.TryGetValue(uuid, out var bleDevice))
            {
                bleDevice.UpdateCBPeripheral(peripheral);
                return bleDevice;
            }

            return _devices[uuid] = new Peripheral(this, uuid, peripheral);
        }

        public override void UpdatedState(CBCentralManager central)
        {
            _owner.FireAdapterChange(this);
        }

        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
        {
            var adv = new Advertisement(GetPeripheral(peripheral), advertisementData, rssi);
            foreach (var scanner in _scanners.ToArray())
                scanner.OnNext(adv);
        }

        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            Debug.WriteLine($"FailedToConnectPeripheral: {peripheral}, {error}");
            GetPeripheral(peripheral).ConnectionFailed(peripheral, error);
        }

        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            Debug.WriteLine($"ConnectedPeripheral: {peripheral}");
            GetPeripheral(peripheral).Connected(peripheral);
        }

        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            Debug.WriteLine($"DisconnectedPeripheral: {peripheral}, {error}");
            GetPeripheral(peripheral).Disconnected(peripheral, error);
        }
    }
}
