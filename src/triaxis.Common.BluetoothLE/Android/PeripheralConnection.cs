using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.Logging;
using Debug = System.Diagnostics.Debug;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE.Android
#else
namespace triaxis.Maui.BluetoothLE.Android
#endif
{
    class PeripheralConnection : BluetoothGattCallback, IPeripheralConnection
    {
        private readonly Peripheral _device;
        private readonly OperationQueue _q;
        private readonly SynchronizationContext _context;
        private readonly string _loggerId;
        internal readonly ILogger _logger;

        private BluetoothGatt _gatt;

        private Task _tDisconnect;
        private Task<IList<IService>> _tServics;

        public event Action<BluetoothGattCharacteristic, byte[]> CharacteristicChanged;
        public event EventHandler<Exception> Closed;
        
        public bool IsConnected => _gatt != null;

        static readonly byte[] s_enableNotification = BluetoothGattDescriptor.EnableNotificationValue.ToArray();
        static readonly byte[] s_disableNotification = BluetoothGattDescriptor.DisableNotificationValue.ToArray();

        public PeripheralConnection(Peripheral device, int num)
        {
            _device = device;
            _context = SynchronizationContext.Current;
            _loggerId = $"BLEConnection:{device.Uuid.RightHalf:X12}:{num}";
            _logger = device.Adapter._loggerFactory.CreateLogger(_loggerId);
            _q = new OperationQueue(_logger);
        }

        public Task<IPeripheralConnection> ConnectAsync(Advertisement reference, int period, int before, int after, int attempts)
            => _q.Enqueue(new ConnectOperation
            {
                _owner = this,
                _reference = (reference?.Time ?? 0) - before,
                _period = period,
                _duration = before + after,
                _attempts = attempts,
            });

        public Task DisconnectAsync()
        {
            if (_q.TryGetCurrent<ConnectOperation>(out var con))
            {
                con.SetCanceled();
                con.Dequeue();
            }

            return _q.EnqueueOnce(ref _tDisconnect, new DisconnectOperation
            {
                _owner = this,
            });
        }

        ValueTask IAsyncDisposable.DisposeAsync()
            => new ValueTask(DisconnectAsync());

        public async Task<string> GetDeviceNameAsync()
        {
            var characteristic = await this.FindServiceCharacteristicAsync(ServiceCharacteristicUuid.GenericAccess.DeviceName);
            if (characteristic == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(await characteristic.ReadAsync());
        }

        public Task<IList<IService>> GetServicesAsync()
            => _q.EnqueueOnce(ref _tServics, new GetServicesOperation
            {
                _owner = this,
            });

        public Task<IList<IService>> GetServicesAsync(params ServiceUuid[] hint)
            => _q.EnqueueOnce(ref _tServics, new GetServicesOperation
            {
                _owner = this,
            });

        public Task<int> RequestMaximumWriteAsync(int request)
            => _q.Enqueue(new MaxWriteRequestOperation
            {
                _owner = this,
                _mtu = request + 3,
            });

        public Task<byte[]> ReadCharacteristicAsync(Characteristic characteristic)
            => _q.Enqueue(new ReadCharacteristicOperation
            {
                _owner = this,
                _ch = characteristic.SystemCharacteristic,
            });

        public Task WriteCharacteristicAsync(Characteristic characteristic, byte[] value, bool withoutResponse)
            => _q.Enqueue(new WriteCharacteristicOperation
            {
                _owner = this,
                _ch = characteristic.SystemCharacteristic,
                _value = value,
                _type = withoutResponse ? GattWriteType.NoResponse : GattWriteType.Default,
            });

        public Task EnableCharacteristicNotificationsAsync(Characteristic characteristic)
             => _q.Enqueue(new EnableNotifyOperation
             {
                 _owner = this,
                 _ch = characteristic.SystemCharacteristic,
                 _desc = characteristic.ClientConfigDescriptor,
             });

        public Task DisableCharacteristicNotificationsAsync(Characteristic characteristic)
            => _q.Enqueue(new DisableNotifyOperation
            {
                _owner = this,
                _ch = characteristic.SystemCharacteristic,
                _desc = characteristic.ClientConfigDescriptor,
            });

        public Task IdleAsync()
            => _q.IdleAsync();

        #region Operations

        internal abstract class Operation<T> : BluetoothLE.Operation<T>
        {
            public PeripheralConnection _owner;
            protected BluetoothGatt _gatt => _owner._gatt;
            private ILogger __logger;
            protected ILogger _logger => __logger ??= _owner._device.Adapter._loggerFactory.CreateLogger($"{_owner._loggerId}:{this}");

            public bool CheckStatus(GattStatus status)
            {
                if (status != GattStatus.Success)
                {
                    SetException($"{BaseErrorMessage}: {status}");
                    return false;
                }
                return true;
            }

            public new bool SetResult(T result)
                => base.SetResult(result);
            public new bool SetCanceled()
                => base.SetCanceled();

            protected override void Start()
            {
                Preflight();
                if (!Execute())
                    SetException(BaseErrorMessage);
            }

            protected virtual void Preflight()
            {
                if (_gatt == null)
                    throw new InvalidOperationException("Not connected");
            }

            protected abstract bool Execute();
            protected abstract string BaseErrorMessage { get; }
        }

        internal class ConnectOperation : Operation<IPeripheralConnection>
        {
            public int _reference, _period, _duration, _attempts;

            protected override void Preflight()
            {
                if (_gatt != null)
                    throw new InvalidOperationException("Already connected");
            }

            protected override bool Execute()
            {
                _owner._device.Adapter.EnqueueConnect(this);
                _owner._tDisconnect = null;
                _owner._tServics = null;
                return true;
            }

            internal void Dequeue()
            {
                _owner._device.Adapter.DequeueConnect(this);
            }

            internal int GetNextAttempt(int after)
            {
                if (_period == 0)
                    return after;

                int align = (after - _reference) % _period;
                int res = after + (align == 0 ? 0 : _period - align);
                _logger.LogTrace("GetNextAttempt({After}) ref={Reference} per={Period} align={Align} res={Res}", after, _reference, _period, align, res);
                return res;
            }

            internal int StartAttempt()
            {
                _owner._gatt = _owner._device.Device.ConnectGatt(Application.Context, false, _owner, BluetoothTransports.Le);
                _logger.LogTrace("_gatt = {Gatt}", _gatt);
                if (_gatt == null)
                {
                    SetException(BaseErrorMessage);
                    Dequeue();
                }

                if (_owner._device.CacheInvalidationRequested())
                {
                    _logger.LogDebug("refreshing service cache");
                    var mth = _gatt.Class.GetMethod("refresh");
                    if (mth == null)
                    {
                        _logger.LogWarning("_gatt.refresh() method not found");
                    }
                    else
                    {
                        var res = mth.Invoke(_gatt);
                        _logger.LogDebug("_gatt.refresh() == {res}", res);
                    }
                }

                return System.Environment.TickCount + _duration;
            }

            internal async Task EndAttempt()
            {
                if (_gatt != null)
                {
                    _logger.LogWarning("connection timeout, disconnecting");
                    _gatt.Disconnect();
                    await System.Threading.Tasks.Task.Delay(200);
                }
                if (_gatt != null)
                {
                    _logger.LogInformation("aborting connection");
                    _gatt.Close();
                    await System.Threading.Tasks.Task.Delay(200);
                }

                if (--_attempts == 0)
                {
                    // done
                    _logger.LogWarning("final attempt failed");
                    SetResult(null);
                    Dequeue();
                }
                else
                {
                    _logger.LogInformation("{AttemptCount} attempts remaining", _attempts);
                }
            }

            internal bool IsConnecting => _gatt != null;

            protected override string BaseErrorMessage => "Failed to start connection";
        }

        class DisconnectOperation : Operation<IPeripheralConnection>
        {
            protected override void Preflight() { }

            protected override bool Execute()
            {
                if (_owner._gatt != null)
                {
                    _owner._gatt.Disconnect();
                    _owner._gatt = null;
                    _logger.LogDebug("_gatt = null (disconnect)");
                }
                else
                {
                    SetResult(_owner);
                }
                return true;
            }

            protected override string BaseErrorMessage => "Failed to disconnect";
        }

        class GetServicesOperation : Operation<IList<IService>>
        {
            protected override bool Execute()
                => _gatt.DiscoverServices();

            protected override string BaseErrorMessage => "Failed to initiate service discovery";
        }

        class MaxWriteRequestOperation : Operation<int>
        {
            public int _mtu;

            protected override bool Execute()
                => _gatt.RequestMtu(_mtu);

            protected override string BaseErrorMessage => $"Failed to request MTU {_mtu}";
        }

        abstract class CharacteristicOperation<T> : Operation<T>
        {
            public BluetoothGattCharacteristic _ch;
        }

        class ReadCharacteristicOperation : CharacteristicOperation<byte[]>
        {
            protected override bool Execute()
                => _gatt.ReadCharacteristic(_ch);

            protected override string BaseErrorMessage => $"Failed to read characteristic {_ch.Uuid.ToUuid()}";
        }

        class WriteCharacteristicOperation : CharacteristicOperation<byte[]>
        {
            public byte[] _value;
            public GattWriteType _type;

            protected override bool Execute()
            {
                _ch.WriteType = _type;
                _ch.SetValue(_value);
                return _gatt.WriteCharacteristic(_ch);
            }

            protected override string BaseErrorMessage => $"Failed to write characteristic {_ch.Uuid.ToUuid()}";
        }

        abstract class NotifyOperation : CharacteristicOperation<bool>
        {
            public BluetoothGattDescriptor _desc;

            public abstract void DescriptorWritten();
        }

        class EnableNotifyOperation : NotifyOperation
        {
            protected override bool Execute()
            {
                if (!_gatt.SetCharacteristicNotification(_ch, true))
                    return false;

                if (_desc == null)
                {
                    _logger.LogWarning("{Characteristic} does not have a client config descriptor", _ch.Uuid.ToUuid());
                    return SetResult(true);
                }

                _desc.SetValue(s_enableNotification);
                return _gatt.WriteDescriptor(_desc);
            }

            protected override string BaseErrorMessage => $"Failed to enable notifications on {_ch.Uuid.ToUuid()}";
            public override void DescriptorWritten() => SetResult(true);
        }

        class DisableNotifyOperation : NotifyOperation
        {
            protected override bool Execute()
            {
                if (_desc == null)
                {
                    _logger.LogWarning("{Characteristic} does not have a client config descriptor", _ch.Uuid.ToUuid());
                    bool res = _gatt.SetCharacteristicNotification(_ch, false);
                    SetResult(false);
                    return res;
                }

                _desc.SetValue(s_disableNotification);
                return _gatt.WriteDescriptor(_desc);
            }

            protected override string BaseErrorMessage => $"Failed to disable notifications on {_ch.Uuid.ToUuid()}";

            public override void DescriptorWritten()
            {
                if (_gatt.SetCharacteristicNotification(_ch, false))
                    SetResult(false);
                else
                    SetException(BaseErrorMessage);
            }
        }

        #endregion

        #region Callbacks

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            _logger.LogDebug("{Gatt} state changed to {NewState}, status {Status}", gatt, newState, status);

            switch (newState)
            {
                case ProfileState.Connected:
                    if (_q.TryGetCurrent<ConnectOperation>(out var con))
                    {
                        _device.AddConnection(this);
                        con.SetResult(this);
                        con.Dequeue();
                    }
                    break;

                case ProfileState.Disconnected:
                    _device.RemoveConnection(this);

                    if (System.Threading.Interlocked.CompareExchange(ref _gatt, null, gatt) != null)
                    {
                        _logger.LogDebug("_gatt = null (lost)");
                    }
                    gatt.Close();

                    Exception err = null;
                    if (_q.TryGetCurrent<DisconnectOperation>(out var op))
                    {
                        // true disconnect request is active
                        op.SetResult(this);
                        err = null;
                    }
                    else if (_q.TryGetCurrent<ConnectOperation>(out var opCon))
                    {
                        // connection failed
                        if (opCon.CheckStatus(status))
                            _device.Adapter.Reschedule();
                    }
                    else
                    {
                        // connection lost, abort whatever operation is current
                        err = new BluetoothLEException($"Connection lost: {status}");
                        _q.Abort(err);
                    }

                    var handler = Closed;
                    if (handler != null)
                    {
                        if (_context != null)
                        {
                            _context.Post(_ => handler(this, err), null);
                        }
                        else
                        {
                            handler(this, err);
                        }
                    }
                    break;
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            if (_q.TryGetCurrent<GetServicesOperation>(out var op))
            {
                if (op.CheckStatus(status))
                    op.SetResult(gatt.Services.SelectArray(s => new Service(this, s)));
            }
            else
            {
                _logger.LogWarning("OnServicesDiscovered({Status}) called without a pending GetServices operation", status);
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            if (_q.TryGetCurrent<ReadCharacteristicOperation>(out var op))
            {
                if (op.CheckStatus(status))
                    op.SetResult(op._ch.GetValue());
            }
            else
            {
                _logger.LogWarning("OnCharacteristicRead({Status}) called without request", status);
            }
        }

        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, [GeneratedEnum] GattStatus status)
        {
            if (_q.TryGetCurrent<WriteCharacteristicOperation>(out var op))
            {
                if (op.CheckStatus(status))
                    op.SetResult(op._value);
            }
            else
            {
                _logger.LogWarning("OnCharacteristicWrite({Status}) called without request", status);
            }
        }

        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            if (_q.TryGetCurrent<NotifyOperation>(out var op))
            {
                if (op.CheckStatus(status))
                    op.DescriptorWritten();
            }
            else
            {
                _logger.LogWarning("OnDescriptorWrite({Status}) called without request", status);
            }
        }

        public override void OnMtuChanged(BluetoothGatt gatt, int mtu, GattStatus status)
        {
            if (_q.TryGetCurrent<MaxWriteRequestOperation>(out var op))
            {
                if (op.CheckStatus(status))
                    op.SetResult(mtu - 3);
            }
            else
            {
                _logger.LogWarning("OnMtuChanged({Mtu}, {Status}) called without request", mtu, status);
            }
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            CharacteristicChanged?.Invoke(characteristic, characteristic.GetValue());
        }

        #endregion

        public override string ToString()
            => $"Connection to {_device}";
    }
}
