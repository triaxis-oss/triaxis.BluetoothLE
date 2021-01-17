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
        private struct ScannerObserver
        {
            public IObserver<IAdvertisement> Observer { get; set; }
            public HashSet<ServiceUuid> Services { get; set; }
        }

        Platform _owner;
        CBCentralManager _central;
        List<ScannerObserver> _scanners = new List<ScannerObserver>();
        Dictionary<Uuid, Peripheral> _devices = new Dictionary<Uuid, Peripheral>();

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

        public IObservable<IAdvertisement> Scan() => ScanImpl(null);
        
        public IObservable<IAdvertisement> Scan(params ServiceUuid[] services) => ScanImpl(new HashSet<ServiceUuid>(services));
        
        private IObservable<IAdvertisement> ScanImpl(HashSet<ServiceUuid> services) => Observable.Create<IAdvertisement>(sub =>
        {
            _scanners.Add(new ScannerObserver { Observer = sub, Services = services });

            UpdateScan();

            return () =>
            {
                _scanners.RemoveAll(scn => scn.Observer == sub);

                UpdateScan();
            };
        });

        void UpdateScan()
        {
            if (_scanners.Count == 0)
            {
                _central.StopScan();
            }
            else
            {
                HashSet<ServiceUuid> services = null;

                if (!_scanners.Any(scn => scn.Services == null))
                {
                    // all scanners want only specific services, create a union over all
                    services = new HashSet<ServiceUuid>();
                    foreach (var scn in _scanners)
                    {
                        services.UnionWith(scn.Services);
                    }
                }

                _central.ScanForPeripherals(services?.Select(uuid => CBUUID.FromBytes(uuid.Uuid.ToByteArrayBE())).ToArray(), new PeripheralScanningOptions
                {
                    AllowDuplicatesKey = true,
                });
            }
        }

        Peripheral GetPeripheral(CBPeripheral peripheral)
        {
            var uuid = peripheral.Identifier.ToUuid();

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
            {
                if (scanner.Services == null ||
                    (adv.Services != null && scanner.Services.Overlaps(adv.Services)))
                {
                    scanner.Observer.OnNext(adv);
                }
            }
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
