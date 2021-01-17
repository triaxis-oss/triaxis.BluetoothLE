using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    class PeripheralConnection : CBPeripheralDelegate, IPeripheralConnection
    {
        readonly Peripheral _device;
        readonly Adapter _adapter;
        readonly CBPeripheral _peripheral;
        readonly CBCentralManager _central;
        Task _tDisconnect;
        Task<IList<IService>> _tServices;
        OperationQueue _q = new OperationQueue();

        public event Action<CBCharacteristic, byte[]> CharacteristicChanged;
        public event EventHandler<Exception> Closed;

        public PeripheralConnection(Peripheral device)
        {
            _device = device;
            _adapter = device.Adapter;
            _peripheral = device.CBPeripheral;
            _central = _adapter.CentralManager;
        }

        public Peripheral Device => _device;
        public Adapter Adapter => _adapter;
        public CBPeripheral Peripheral => _peripheral;
        public CBCentralManager CentralManager => _central;

        public Task<IPeripheralConnection> ConnectAsync(int timeout)
            => _q.Enqueue(new ConnectOperation
            {
                _owner = this,
            }, timeout);

        public Task DisconnectAsync()
        {
            if (_q.TryGetCurrent<ConnectOperation>(out var con))
                con.Cancel();

            return _q.EnqueueOnce(ref _tDisconnect, new DisconnectOperation
            {
                _owner = this,
            });
        }

        public Task<IList<IService>> GetServicesAsync()
            => _q.EnqueueOnce(ref _tServices, new GetServicesOperation
            {
                _owner = this,
            });

        internal Task<IList<ICharacteristic>> GetCharacteristicsAsync(Service service)
            => _q.EnqueueOnce(ref service._tCharacteristics, new GetCharacteristicsOperation
            {
                _owner = this,
                _service = service,
            });

        public Task<int> RequestMaximumWriteAsync(int request)
        {
            int max = (int)_peripheral.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse);
            return Task.FromResult(Math.Min(request, max));
        }

        internal Task<byte[]> ReadCharacteristicAsync(Characteristic characteristic)
            => _q.Enqueue(new ReadCharacteristicOperation
            {
                _owner = this,
                _ch = characteristic.CBCharacteristic,
            });

        internal Task WriteCharacteristicAsync(Characteristic characteristic, byte[] data, bool withoutResponse)
            => _q.Enqueue(new WriteCharacteristicOperation
            {
                _owner = this,
                _ch = characteristic.CBCharacteristic,
                _data = data,
                _type = withoutResponse ? CBCharacteristicWriteType.WithoutResponse : CBCharacteristicWriteType.WithResponse,
            });

        internal Task SetCharacteristicNotificationsAsync(Characteristic characteristic, bool enable)
            => _q.Enqueue(new NotifyOperation
            {
                _owner = this,
                _ch = characteristic.CBCharacteristic,
                _enable = enable,
            });

        public Task IdleAsync()
            => _q.IdleAsync();

        #region Operations

        abstract class Operation<TRes> : BluetoothLE.Operation<TRes>
        {
            public PeripheralConnection _owner;
            protected CBPeripheral _peripheral => _owner._peripheral;

            public bool CheckSuccess(NSError error)
            {
                if (error == null)
                    return true;

                SetException(new NSErrorException(error));
                return false;
            }

            public new bool SetResult(TRes result)
                => base.SetResult(result);
            public new bool SetException(Exception error)
                => base.SetException(error);

            public override CancellationTokenRegistration Start(CancellationToken cancellationToken)
            {
                Preflight();
                Execute();
                return !Task.IsCompleted && cancellationToken.CanBeCanceled ?
                    SetupCancellation(cancellationToken) :
                    default;
            }

            protected virtual void Preflight()
            {
                if (!_peripheral.IsConnected)
                    throw new InvalidOperationException("Not connected");
            }

            protected abstract void Execute();

            protected virtual CancellationTokenRegistration SetupCancellation(CancellationToken token)
                => default;
        }

        class ConnectOperation : Operation<IPeripheralConnection>
        {
            protected override void Preflight()
            {
                if (_peripheral.IsConnected)
                    throw new InvalidOperationException("Already connected");
            }

            protected override void Execute()
            {
                _owner._tDisconnect = null;
                _peripheral.Delegate = _owner;
                _owner._central.ConnectPeripheral(_peripheral);
            }

            protected override CancellationTokenRegistration SetupCancellation(CancellationToken token)
                => token.Register(() =>
                {
                    Debug.WriteLine($"BLEConnection connect timeout: {_peripheral}");
                    _owner._central.CancelPeripheralConnection(_peripheral);
                }, true);
        }

        class DisconnectOperation : Operation<IPeripheralConnection>
        {
            protected override void Preflight() { }

            protected override void Execute() => _owner._central.CancelPeripheralConnection(_peripheral);
        }

        class GetServicesOperation : Operation<IList<IService>>
        {
            protected override void Execute() => _peripheral.DiscoverServices();
        }

        class GetCharacteristicsOperation : Operation<IList<ICharacteristic>>
        {
            public Service _service;

            protected override void Execute() => _peripheral.DiscoverCharacteristics(_service.CBService);
        }

        abstract class CharacteristicOperation<T> : Operation<T>
        {
            public CBCharacteristic _ch;

            public void Result(NSError error)
            {
                if (CheckSuccess(error))
                    SetResult(GetResultValue());
            }

            protected abstract T GetResultValue();
        }

        class ReadCharacteristicOperation : CharacteristicOperation<byte[]>
        {
            protected override void Execute() => _peripheral.ReadValue(_ch);
            protected override byte[] GetResultValue() => _ch.Value.ToArray();
        }

        class WriteCharacteristicOperation : CharacteristicOperation<byte[]>
        {
            public byte[] _data;
            public CBCharacteristicWriteType _type;

            protected override void Execute()
            {
                _peripheral.WriteValue(NSData.FromArray(_data), _ch, _type);
                if (_type == CBCharacteristicWriteType.WithoutResponse && _peripheral.CanSendWriteWithoutResponse)
                {
                    // another write can be queued, meaning there's no need to wait for the notification
                    SetResult(_data);
                }
            }

            protected override byte[] GetResultValue() => _data;
        }

        class NotifyOperation : CharacteristicOperation<bool>
        {
            public bool _enable;

            protected override void Execute() => _peripheral.SetNotifyValue(_enable, _ch);
            protected override bool GetResultValue() => _enable;
        }

        #endregion

        #region Callbacks

        internal bool Connected(CBPeripheral peripheral)
        {
            if (_q.TryGetCurrent<ConnectOperation>(out var op))
            {
                Debug.WriteLine($"BLEConnection active: {peripheral}");
                _peripheral.Delegate = this;
                return op.SetResult(this);
            }
            else
            {
                Debug.WriteLine($"BLEConnection received unexpected Connected notification");
                return false;
            }
        }

        internal bool ConnectionFailed(CBPeripheral peripheral, NSError error)
        {
            if (_q.TryGetCurrent<ConnectOperation>(out var op))
            {
                Debug.WriteLine($"BLEConnection failed: {peripheral}, {error}");
                if (!op.CheckSuccess(error))
                    op.SetException(new BluetoothLEException("Unknown error occured while connecting"));

                Closed?.Invoke(this, null);
                return true;
            }
            else
            {
                Debug.WriteLine($"BLEConnection received unexpected ConnectionFailed notification: {peripheral}, {error}");
                return false;
            }
        }

        internal bool Disconnected(CBPeripheral peripheral, NSError error)
        {
            if (_peripheral == peripheral && _peripheral.Delegate == this)
            {
                Debug.WriteLine($"BLEConnection closed: {peripheral}, {error}");
                _peripheral.Delegate = null;

                if (_q.TryGetCurrent<DisconnectOperation>(out var op))
                {
                    if (op.CheckSuccess(error))
                        op.SetResult(this);
                }
                else if (_q.TryGetCurrent<ConnectOperation>(out var opCon))
                {
                    if (opCon.CheckSuccess(error))
                        opCon.SetResult(null);
                }
                else
                {
                    _q.Abort(new NSErrorException(error));
                }

                Closed?.Invoke(this, error == null ? null : new NSErrorException(error));

                return true;
            }
            else
            {
                Debug.WriteLine($"BLEConnection received unexpected Disconnected notification: {peripheral}, {error}");
                return false;
            }
        }

        public override void DiscoveredService(CBPeripheral peripheral, NSError error)
        {
            if (_q.TryGetCurrent<GetServicesOperation>(out var op))
            {
                if (op.CheckSuccess(error))
                    op.SetResult(Array.ConvertAll(_peripheral.Services, cbSvc => new Service(this, cbSvc)));
            }
        }

        public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError error)
        {
            if (_q.TryGetCurrent<GetCharacteristicsOperation>(out var op) && op._service.CBService == service)
            {
                if (op.CheckSuccess(error))
                    op.SetResult(Array.ConvertAll(service.Characteristics, cbCh => new Characteristic(op._service, cbCh)));
            }
        }

        public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
        {
            if (_q.TryGetCurrent<ReadCharacteristicOperation>(out var op) && op._ch == characteristic)
            {
                op.Result(error);
            }
            else
            {
                CharacteristicChanged?.Invoke(characteristic, characteristic.Value.ToArray());
            }
        }

        public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
        {
            if (_q.TryGetCurrent<WriteCharacteristicOperation>(out var op) && op._ch == characteristic)
            {
                op.Result(error);
            }
        }

        public override void IsReadyToSendWriteWithoutResponse(CBPeripheral peripheral)
        {
            if (_q.TryGetCurrent<WriteCharacteristicOperation>(out var op))
            {
                op.Result(null);
            }
        }

        public override void UpdatedNotificationState(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
        {
            if (_q.TryGetCurrent<NotifyOperation>(out var op))
            {
                op.Result(error);
            }
        }

        #endregion
    }
}
