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

using Environment = System.Environment;
using NativeState = Android.Bluetooth.State;
using ScanMode = Android.Bluetooth.LE.ScanMode;

namespace triaxis.Xamarin.BluetoothLE.Android
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
        ScanSettings _scanSettings;
        ScanCallbackImpl _scanCallback;
        ScanObserver[] _scanObservers;
        List<PeripheralConnection.ConnectOperation> _connectOps;

        public Adapter(BluetoothAdapter adapter)
        {
            _adapter = adapter;
            _peripherals = new Dictionary<Uuid, Peripheral>();
            _scanObservers = Array.Empty<ScanObserver>();
            _connectOps = new List<PeripheralConnection.ConnectOperation>();

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
                .SetScanMode(ScanMode.LowLatency)
                .SetReportDelay(100);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                sb = sb.SetMatchMode(BluetoothScanMatchMode.Aggressive).SetNumOfMatches(1);
            }

            _scanSettings = sb.Build();
            _scanner = _adapter.BluetoothLeScanner;
            _scanCallback = new ScanCallbackImpl(this);
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
                ProcessResult(result);
            }

            public override void OnBatchScanResults(IList<ScanResult> results)
            {
                foreach (var res in results)
                {
                    ProcessResult(res);
                }
            }

            private void ProcessResult(ScanResult result)
            {
                var adv = new Advertisement(_adapter.GetPeripheral(result.Device), result.Rssi, -127, result.ScanRecord.GetBytes(), result.TimestampNanos);
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

        void StartScan()
        {
            _scanning = true;
            try
            {
                _scanner.StartScan(null, _scanSettings, _scanCallback);
            }
            catch (Exception err)
            {
                global::Android.Util.Log.Error("BLEAdapter", "StartScan Crashed: " + err);
            }
        }

        void StopScan()
        {
            _scanning = false;
            try
            {
                _scanner.StopScan(_scanCallback);
            }
            catch (Exception err)
            {
                global::Android.Util.Log.Error("BLEAdapter", "StopScan Crashed: " + err);
            }
        }

        public IObservable<IAdvertisement> Scan() => ScanImpl(null);
        
        public IObservable<IAdvertisement> Scan(params ServiceUuid[] services) => ScanImpl(new HashSet<ServiceUuid>(services));
        
        private IObservable<IAdvertisement> ScanImpl(HashSet<ServiceUuid> services) => Observable.Create<IAdvertisement>(sub =>
        {
            _scanObservers = _scanObservers.Append(new ScanObserver { Observer = sub, Services = services });
            Reschedule();

            return () =>
            {
                _scanObservers = _scanObservers.Remove(x => x.Observer == sub);
                Reschedule();
            };
        });

        #region Scan/collection scheduler
        TaskCompletionSource<bool> _tcsSchedule = new TaskCompletionSource<bool>();

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

        private static void Log(string log)
            => global::Android.Util.Log.Info("BLEScheduler", log);

        private async Task<bool> Schedule()
        {
            // reset trigger
            _tcsSchedule = new TaskCompletionSource<bool>();

            Log("Scheduling...");

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
                    Log($"Starting scanner until {conTime - ScanStopDelay} before connecting");
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
                    Log($"Stopping scanner, {conOp}");
                }
                else
                {
                    Log("Stopping scanner, no observers left");
                }
                // stop scanning
                StopScan();
                // unconditional delay
                await Task.Delay(ScanStopDelay);
            }

            if (conOp != null)
            {
                Log($"Waiting until {conTime} for {conOp}");
                // we are going to connect
                if (await Delay(conTime - Environment.TickCount))
                {
                    // reschedule
                    return true;
                }

                Log($"Starting {conOp} attempt");
                int endOfAttempt = conOp.StartAttempt();
                while (await Delay(endOfAttempt - Environment.TickCount))
                {
                    if (conOp.Task.IsCompleted)
                    {
                        // connection completed one way or another, we can go schedule again
                        Log($"{conOp} complete");
                        return true;
                    }
                    if (!conOp.IsConnecting)
                    {
                        // connection is no longer pending (probably failed), we can schedule something else
                        break;
                    }
                }
                // abort connection attempt and schedule something else
                Log($"Ending {conOp} attempt");
                await conOp.EndAttempt();
                return true;
            }

            // nothing to connect, we can start scanning
            if (!_scanning && _scanObservers.Length > 0 && !ActiveConnection)
            {
                for (; ; )
                {
                    Log("Starting scanner");
                    StartScan();
                    if (await Delay(ScanContinuousTime))
                    {
                        return true;
                    }
                    Log("No event, interrupting scan for a while");
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
