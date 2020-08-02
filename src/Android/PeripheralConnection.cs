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

using Debug = System.Diagnostics.Debug;

namespace triaxis.Xamarin.BluetoothLE.Android
{
    class PeripheralConnection : BluetoothGattCallback, IPeripheralConnection
    {
        Peripheral _device;
        BluetoothGatt _gatt;
        Task _tDisconnect;
        Task<IList<IService>> _tServics;
        OperationQueue _q = new OperationQueue();
        SynchronizationContext _context;

        public event Action<BluetoothGattCharacteristic, byte[]> CharacteristicChanged;
        public event EventHandler<Exception> Closed;

        static readonly byte[] s_enableNotification = BluetoothGattDescriptor.EnableNotificationValue.ToArray();
        static readonly byte[] s_disableNotification = BluetoothGattDescriptor.DisableNotificationValue.ToArray();

        public PeripheralConnection(Peripheral device)
        {
            _device = device;
            _context = SynchronizationContext.Current;
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
                con.Cancel();
                con.Dequeue();
            }

            return _q.EnqueueOnce(ref _tDisconnect, new DisconnectOperation
            {
                _owner = this,
            });
        }

        public Task<IList<IService>> GetServicesAsync()
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

            public override CancellationTokenRegistration Start(CancellationToken cancellationToken)
            {
                Preflight();
                if (!Execute())
                    SetException(BaseErrorMessage);

                return !Task.IsCompleted && cancellationToken.CanBeCanceled ?
                    SetupCancellation(cancellationToken) :
                    default;
            }

            protected virtual void Preflight()
            {
                if (_gatt == null)
                    throw new InvalidOperationException("Not connected");
            }

            protected virtual CancellationTokenRegistration SetupCancellation(CancellationToken token)
                => default;

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
                Debug.WriteLine($"{this} GetNextAttempt({after}) ref={_reference} per={_period} align={align} res={res}");
                return res;
            }

            internal int StartAttempt()
            {
                _owner._gatt = _owner._device.Device.ConnectGatt(Application.Context, false, _owner, BluetoothTransports.Le);
                Debug.WriteLine($"{this} _gatt = {_gatt}");
                if (_gatt == null)
                {
                    SetException(BaseErrorMessage);
                    Dequeue();
                }

                if (_owner._device.CacheInvalidationRequested())
                {
                    Debug.WriteLine($"{this} refreshing service cache");
                    var mth = _gatt.Class.GetMethod("refresh");
                    if (mth == null)
                    {
                        Debug.WriteLine($"{this} _gatt.refresh() method not found");
                    }
                    else
                    {
                        var res = mth.Invoke(_gatt);
                        Debug.WriteLine($"{this} _gatt.refresh() == {res}");
                    }
                }

                return System.Environment.TickCount + _duration;
            }

            internal async Task EndAttempt()
            {
                if (_gatt != null)
                {
                    Debug.WriteLine($"{this} connection timeout, disconnecting");
                    _gatt.Disconnect();
                    await System.Threading.Tasks.Task.Delay(200);
                }
                if (_gatt != null)
                {
                    Debug.WriteLine($"{this} closing connection");
                    _gatt.Close();
                    await System.Threading.Tasks.Task.Delay(200);
                }

                if (--_attempts == 0)
                {
                    // done
                    Debug.WriteLine($"{this} final attempt failed");
                    SetResult(null);
                    Dequeue();
                }
                else
                {
                    Debug.WriteLine($"{this} {_attempts} attempts remaining");
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
                    Debug.WriteLine($"{this} _gatt = null (disconnect)");
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

            protected override string BaseErrorMessage => $"Failed to read characteristic {_ch}";
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

            protected override string BaseErrorMessage => $"Failed to write characteristic {_ch}";
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
                    Debug.WriteLine($"{_ch} does not have a client config descriptor");
                    return SetResult(true);
                }

                _desc.SetValue(s_enableNotification);
                return _gatt.WriteDescriptor(_desc);
            }

            protected override string BaseErrorMessage => $"Failed to enable notifications on {_ch}";
            public override void DescriptorWritten() => SetResult(true);
        }

        class DisableNotifyOperation : NotifyOperation
        {
            protected override bool Execute()
            {
                if (_desc == null)
                {
                    Debug.WriteLine($"{_ch} does not have a client config descriptor");
                    bool res = _gatt.SetCharacteristicNotification(_ch, false);
                    SetResult(false);
                    return res;
                }

                _desc.SetValue(s_disableNotification);
                return _gatt.WriteDescriptor(_desc);
            }

            protected override string BaseErrorMessage => $"Failed to disable notifications on {_ch}";

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
            Debug.WriteLine($"{gatt} state changed to {newState}, status {status}");

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
                        Debug.WriteLine($"{this} _gatt = null (lost)");
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
                Debug.WriteLine($"OnServicesDiscovered({status}) called without a pending GetServices operation");
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
                Debug.WriteLine($"OnCharacteristicRead({status}) called without request");
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
                Debug.WriteLine($"OnCharacteristicWrite({status}) called without request");
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
                Debug.WriteLine($"OnDescriptorWrite({status}) called without request");
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
                Debug.WriteLine($"OnMtuChanged({mtu}, {status}) called without request");
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
