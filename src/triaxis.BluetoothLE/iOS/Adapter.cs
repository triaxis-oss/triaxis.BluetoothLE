using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace triaxis.BluetoothLE
{
    class Adapter : CBCentralManagerDelegate, IAdapter
    {
        private struct ScannerObserver
        {
            public IObserver<IAdvertisement> Observer { get; set; }
            public HashSet<ServiceUuid> Services { get; set; }
        }

        private readonly Platform _owner;
        private readonly CBCentralManager _central;
        private readonly List<ScannerObserver> _scanners = new List<ScannerObserver>();
        private readonly Dictionary<Uuid, PeripheralWrapper> _devices = new Dictionary<Uuid, PeripheralWrapper>();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public Adapter(Platform owner, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger("BLEAdapter");
            _owner = owner;
            _central = new CBCentralManager(this, null);
        }

        public ILogger CreateLogger(string name)
            => _loggerFactory.CreateLogger(name);

        public AdapterState State
        {
            get
            {
                switch (((CBManager)_central).State)
                {
                    case CBManagerState.PoweredOff: return AdapterState.Off;
                    case CBManagerState.PoweredOn: return AdapterState.On;
                    case CBManagerState.Resetting: return AdapterState.Transitioning;
                    case CBManagerState.Unauthorized: return AdapterState.Unauthorized;
                    case CBManagerState.Unsupported: return AdapterState.Unsupported;
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
            if (peripheral.Delegate is Peripheral p)
            {
                return p;
            }

            var uuid = peripheral.Identifier.ToUuid();

            if (!_devices.TryGetValue(uuid, out var wrapper))
            {
                _devices[uuid] = wrapper = new PeripheralWrapper(this, uuid);
            }

            peripheral.Delegate = p = wrapper.CreatePeripheral(peripheral);
            return p;
        }

        public override void UpdatedState(CBCentralManager central)
        {
            _owner.FireAdapterChange(this);
        }

        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
        {
            var adv = new Advertisement(GetPeripheral(peripheral).Wrapper, advertisementData, rssi);
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
            _logger.LogDebug("FailedToConnectPeripheral: {Peripheral}, {Error}", peripheral, error);
            GetPeripheral(peripheral).OnConnectionFailed(error);
        }

        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            _logger.LogDebug("ConnectedPeripheral: {Peripheral}", peripheral);
            GetPeripheral(peripheral).OnConnected();
        }

        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            _logger.LogDebug("DisconnectedPeripheral: {Peripheral}, {Error}", peripheral, error);
            GetPeripheral(peripheral).OnDisconnected(error);
        }
    }
}
