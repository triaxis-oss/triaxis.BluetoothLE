using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.Logging;
using Environment = System.Environment;
using NativeState = Android.Bluetooth.State;
using ScanMode = Android.Bluetooth.LE.ScanMode;

namespace triaxis.BluetoothLE
{
    class Adapter : IAdapter
    {
        private struct ScanObserver
        {
            public IObserver<IAdvertisement> Observer { get; set; }
            public HashSet<ServiceUuid> Services { get; set; }
        }

        BluetoothAdapter _adapter;
        Dictionary<Uuid, Peripheral> _peripherals;

        bool _scanning;
        BluetoothLeScanner _scanner;
        ScanSettings _scanSettings, _scanSettingsBatch;
        ScanCallback _scanCallback, _scanCallbackBatch;
        ScanFilter[] _scanFilters;
        ScanObserver[] _scanObservers;
        List<PeripheralConnection.ConnectOperation> _connectOps;
        internal readonly ILoggerFactory _loggerFactory;
        ILogger _logger;

        public Adapter(BluetoothAdapter adapter, ILoggerFactory loggerFactory)
        {
            _adapter = adapter;
            _peripherals = new Dictionary<Uuid, Peripheral>();
            _scanObservers = Array.Empty<ScanObserver>();
            _connectOps = new List<PeripheralConnection.ConnectOperation>();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger("BLEAdapter");

            InitializeScanner();
            Scheduler();
        }

        void InitializeScanner()
        {
            if (_adapter == null)
            {
                return;
            }

            // initialize scanner
            var sb = new ScanSettings.Builder()
                .SetCallbackType(ScanCallbackType.AllMatches)
                .SetScanMode(ScanMode.LowLatency);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                sb = sb.SetMatchMode(BluetoothScanMatchMode.Aggressive).SetNumOfMatches(1);
            }

            _scanSettings = sb.Build();
            sb.SetReportDelay(1000);
            _scanSettingsBatch = sb.Build();

            _scanner = _adapter.BluetoothLeScanner;
            _scanCallback = new ScanCallbackImpl(this);
            _scanCallbackBatch = new BatchScanCallbackImpl();
        }

        public AdapterState State
        {
            get
            {
                if (_adapter == null)
                    return AdapterState.Unsupported;

                switch (_adapter.State)
                {
                    case NativeState.Off: return AdapterState.Off;
                    case NativeState.On: return AdapterState.On;
                    case NativeState.Connected: return AdapterState.On;
                    case NativeState.Connecting: return AdapterState.Transitioning;
                    case NativeState.Disconnected: return AdapterState.On;
                    case NativeState.Disconnecting: return AdapterState.Transitioning;
                    case NativeState.TurningOn: return AdapterState.Transitioning;
                    case NativeState.TurningOff: return AdapterState.Transitioning;
                    default: return AdapterState.Unknown;
                }
            }
        }

        Peripheral GetPeripheral(BluetoothDevice device)
        {
            var uuid = new Uuid(0, Convert.ToUInt64(device.Address.Replace(":", ""), 16));

            if (_peripherals.TryGetValue(uuid, out var peripheral))
            {
                peripheral.UpdateDevice(device);
                return peripheral;
            }

            return _peripherals[uuid] = new Peripheral(this, uuid, device);
        }

        class ScanCallbackImpl : ScanCallback
        {
            public ScanCallbackImpl(Adapter adapter)
            {
                _adapter = adapter;
            }

            Adapter _adapter;

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
            {
                _adapter._advSeen = true;
                ProcessResult(result);
            }

            private void ProcessResult(ScanResult result)
            {
                var adv = new Advertisement(_adapter.GetPeripheral(result.Device), result.Rssi, -127, result.ScanRecord.GetBytes(), DateTime.UtcNow);
                Array.ForEach(_adapter._scanObservers, obs =>
                {
                    if (obs.Services == null || (adv.Services != null && obs.Services.Overlaps(adv.Services)))
                    {
                        obs.Observer.OnNext(adv);
                    }
                });
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
                var err = new ScanException(errorCode);
                Array.ForEach(_adapter._scanObservers, obs => obs.Observer.OnError(err));
            }
        }

        class BatchScanCallbackImpl : ScanCallback
        {
            public BatchScanCallbackImpl()
            {
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
            {
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
            }
        }

        void StartScan()
        {
            _scanning = true;
            try
            {
                _scanner.StartScan(_scanFilters, _scanSettings, _scanCallback);
                _scanner.StartScan(_scanFilters, _scanSettingsBatch, _scanCallbackBatch);
            }
            catch (Exception err)
            {
                _logger.LogError(err, "StartScan Crashed");
            }
        }

        void StopScan()
        {
            _scanning = false;
            try
            {
                _scanner.StopScan(_scanCallback);
                _scanner.StopScan(_scanCallbackBatch);
            }
            catch (Exception err)
            {
                _logger.LogError(err, "StopScan Crashed");
            }
        }

        public IObservable<IAdvertisement> Scan() => ScanImpl(null);
        
        public IObservable<IAdvertisement> Scan(params ServiceUuid[] services) => ScanImpl(new HashSet<ServiceUuid>(services));
        
        private IObservable<IAdvertisement> ScanImpl(HashSet<ServiceUuid> services) => Observable.Create<IAdvertisement>(sub =>
        {
            _scanObservers = _scanObservers.Append(new ScanObserver { Observer = sub, Services = services });
            UpdateScanFilters();

            return () =>
            {
                _scanObservers = _scanObservers.Remove(x => x.Observer == sub);
                UpdateScanFilters();
            };
        });

        void UpdateScanFilters()
        {
            if (_scanObservers.Any(obs => obs.Services == null))
            {
                _scanFilters = null;
            }
            else
            {
                var builder = new ScanFilter.Builder();
                _scanFilters = _scanObservers
                    .Aggregate(new HashSet<ServiceUuid>(), (acc, elem) => { acc.UnionWith(elem.Services); return acc; })
                    .Select(svc =>
                    {
                        builder.SetServiceUuid(svc.Uuid.ToParcelUuid());
                        return builder.Build();
                    })
                    .ToArray();
            }
            
            Reschedule();
        }

        #region Scan/collection scheduler
        TaskCompletionSource<bool> _tcsSchedule = new TaskCompletionSource<bool>();
        bool _advSeen;

        internal void Reschedule()
        {
            _tcsSchedule.TrySetResult(true);
        }

        private async void Scheduler()
        {
            for (; ; )
            {
                await Delay(int.MinValue);
                while (await Schedule()) { }
            }
        }

        internal void EnqueueConnect(PeripheralConnection.ConnectOperation con)
        {
            _connectOps.Add(con);
            Reschedule();
        }

        internal void DequeueConnect(PeripheralConnection.ConnectOperation con)
        {
            _connectOps.Remove(con);
            Reschedule();
        }

        /// <summary>
        /// Delays for the specified number of milliseconds, unless a reschedule is requested
        /// </summary>
        /// <returns>true if the delay was interrupted by a scheduling request; false otherwise</returns>
        private async Task<bool> Delay(int ms)
        {
            if (ms == int.MinValue)
            {
                // infinite wait, simply wait for the schedule task to complete
                await _tcsSchedule.Task;
            }

            if (!_tcsSchedule.Task.IsCompleted)
            {
                if (ms <= 0)
                {
                    // no delay, return immediately
                    return false;
                }

                using (var delayCancel = new CancellationTokenSource())
                {
                    var delay = Task.Delay(ms, delayCancel.Token);
                    var res = await Task.WhenAny(_tcsSchedule.Task, delay);
                    delayCancel.Cancel();
                    if (res == delay)
                    {
                        // time has passed and no reschedule request has arrived
                        return false;
                    }
                }
            }

            return true;
        }

        const int ScanStopDelay = 100;
        const int ScanContinuousTime = 10000;
        const int ScanInterruptionTime = 1000;

        bool Scanning => _scanning;
        bool WantScan => _scanObservers.Length > 0;
        bool ActiveConnection => _peripherals.Values.Any(dev => dev.IsConnected);

        private async Task<bool> Schedule()
        {
            // reset trigger
            _tcsSchedule = new TaskCompletionSource<bool>();

            _logger.LogInformation("Scheduling...");

            // look for an active connection request
            PeripheralConnection.ConnectOperation conOp = null;
            int conTime = 0;
            int conAfter = Environment.TickCount + (_scanning ? ScanStopDelay : 0);

            foreach (var con in _connectOps)
            {
                int t = con.GetNextAttempt(conAfter);
                if (conOp == null || (t - conTime) < 0)
                {
                    conOp = con;
                    conTime = t;
                }
            }

            conAfter = conTime - Environment.TickCount;
            if (WantScan && conOp != null && conAfter > ScanStopDelay && !ActiveConnection)
            {
                if (!Scanning)
                {
                    _logger.LogDebug("Starting scanner until {Ticks} before connecting", conTime - ScanStopDelay);
                    StartScan();
                }
                // wait until it's time to connect
                if (await Delay(conTime - Environment.TickCount - ScanStopDelay))
                {
                    return true;
                }
            }

            if (Scanning && (conOp != null || !WantScan))
            {
                if (conOp != null)
                {
                    _logger.LogInformation("Stopping scanner due to {Operation}", conOp);
                }
                else
                {
                    _logger.LogInformation("Stopping scanner, no observers left");
                }
                // stop scanning
                StopScan();
                // unconditional delay
                await Task.Delay(ScanStopDelay);
            }

            if (conOp != null)
            {
                _logger.LogInformation("Waiting until {Ticks} for {Operation}", conTime, conOp);
                // we are going to connect
                if (await Delay(conTime - Environment.TickCount))
                {
                    // reschedule
                    return true;
                }

                _logger.LogInformation("Starting {Operation} attempt", conOp);
                int endOfAttempt = conOp.StartAttempt();
                while (await Delay(endOfAttempt - Environment.TickCount))
                {
                    if (conOp.Task.IsCompleted)
                    {
                        // connection completed one way or another, we can go schedule again
                        _logger.LogInformation("{Operation} complete", conOp);
                        return true;
                    }
                    if (!conOp.IsConnecting)
                    {
                        // connection is no longer pending (probably failed), we can schedule something else
                        break;
                    }
                }
                // abort connection attempt and schedule something else
                _logger.LogInformation("Ending {Operation} attempt", conOp);
                await conOp.EndAttempt();
                return true;
            }

            // nothing to connect, we can start scanning
            if (!_scanning && _scanObservers.Length > 0 && !ActiveConnection)
            {
                for (; ; )
                {
                    _logger.LogInformation("Starting scanner");
                    StartScan();
                    for (; ; )
                    {
                        _advSeen = false;
                        if (await Delay(ScanContinuousTime))
                        {
                            return true;
                        }
                        if (!_advSeen)
                        {
                            break;
                        }
                    }
                    _logger.LogInformation("No event, interrupting scan for a while");
                    StopScan();
                    if (await Delay(ScanInterruptionTime))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
