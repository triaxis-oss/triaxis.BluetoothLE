using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE.iOS
#else
namespace triaxis.Maui.BluetoothLE.iOS
#endif
{
    partial class Peripheral
    {
        interface IPeripheralOperation : IOperation
        {
            ConnectionInstance Owner { get; }
            Connection Connection { get; }
            Peripheral Peripheral { get; }

            void HandleDisconnect(NSError error);
        }

        abstract class Operation<TRes> : BluetoothLE.Operation<TRes>, IPeripheralOperation
        {
            public ConnectionInstance Owner { get; private set; }
            public Connection Connection => Owner?.Connection;
            public Peripheral Peripheral { get; private set; }
            public CBPeripheral CBPeripheral => Peripheral?.CBPeripheral;

            internal void Bind(ConnectionInstance owner)
            {
                System.Diagnostics.Debug.Assert(Owner == null);
                Owner = owner;
            }

            internal void Bind(Peripheral p)
            {
                System.Diagnostics.Debug.Assert(Peripheral == null);
                Peripheral = p;
            }

            public bool CheckSuccess(NSError error)
            {
                if (error == null)
                    return true;

                SetException(error.ToException());
                return false;
            }

            public new bool SetResult(TRes result)
                => base.SetResult(result);
            public new bool SetException(Exception error)
                => base.SetException(error);
            public new bool SetCanceled()
                => base.SetCanceled();

            protected void RequireConnected()
            {
                if (!Connection.IsActive)
                {
                    throw new InvalidOperationException("Not connected");
                }
            }

            public void HandleDisconnect(NSError error)
            {
                var exception = error.ToException();
                Connection.OnClosed(exception);
                if (exception == null)
                {
                    HandleDisconnectWithoutError();
                }
                else
                {
                    SetException(exception);
                }
            }

            public virtual void HandleDisconnectWithoutError()
            {
                SetException(new BluetoothLEException("Connection lost"));
            }
        }

        class ConnectOperation : Operation<IPeripheralConnection>
        {
            protected override void Start()
            {
                if (Connection.IsActive)
                {
                    switch (Connection.State)
                    {
                        case CBPeripheralState.Connected:
                            // connection is already active, just reuse
                            SetResult(Owner);
                            return;

                        case CBPeripheralState.Disconnected:
                            // start the connection
                            Peripheral.CBConnect();
                            return;
                    }
                }

                // this can happen e.g. when a connection is being reused
                // but fails before getting to this operation
                SetException(new InvalidOperationException("Connection is in an invalid state"));
            }

            protected override void OnTimeout()
            {
                Owner.OnClosed(null);
                SetResult(null);
            }

            public override void HandleDisconnectWithoutError()
            {
                SetResult(null);
            }
        }

        class DisconnectOperation : Operation<IPeripheralConnection>
        {
            protected override void Start()
            {
                Owner.OnClosed(null);
                SetResult(Owner);
            }

            public override void HandleDisconnectWithoutError()
            {
                // TODO: not sure if this can even happen...
                SetResult(Owner);
            }
        }

        class GetServicesOperation : Operation<bool>
        {
            private readonly ServiceUuid[] _hint;

            public GetServicesOperation(ServiceUuid[] hint = null)
            {
                _hint = hint;
            }

            protected override void Start()
            {
                RequireConnected();

                if (_hint?.Length > 0)
                {
                    CBPeripheral.DiscoverServices(_hint.Select(uuid => CBUUID.FromBytes(uuid.Uuid.ToByteArrayBE())).ToArray());
                }
                else
                {
                    CBPeripheral.DiscoverServices();
                }
            }

            internal void DidDiscoverServices()
            {
                SetResult(true);
            }
        }

        class GetCharacteristicsOperation : Operation<IList<ICharacteristic>>
        {
            public GetCharacteristicsOperation(Service service)
            {
                Service = service;
                LogScope["Service"] = service.Uuid.ToString();
            }

            public Service Service { get; }
            private CBService CBService => Service.CBService;

            protected override void Start()
            {
                if (CBService.Characteristics != null)
                {
                    DidDiscoverCharacteristics();
                }
                else
                {
                    CBPeripheral.DiscoverCharacteristics(CBService);
                }
            }

            internal void DidDiscoverCharacteristics()
            {
                SetResult(Array.ConvertAll(CBService.Characteristics, ch => Owner.CreateCharacteristic(Service, ch)));
            }
        }

        abstract class CharacteristicOperation<T> : Operation<T>
        {
            public CharacteristicOperation(Characteristic characteristic)
            {
                Characteristic = characteristic;
                LogScope["Characteristic"] = characteristic.Uuid;
            }

            public Characteristic Characteristic { get; }
            public CBCharacteristic CBCharacteristic => Characteristic?.CBCharacteristic;

            public virtual void Result(NSError error)
            {
                if (CheckSuccess(error))
                {
                    SetResult(GetResultValue());
                }
            }

            protected abstract T GetResultValue();
        }

        class ReadCharacteristicOperation : CharacteristicOperation<byte[]>
        {
            public ReadCharacteristicOperation(Characteristic characteristic)
                : base(characteristic)
            {
            }

            protected override void Start()
            {
                RequireConnected();
                CBPeripheral.ReadValue(CBCharacteristic);
            }

            protected override byte[] GetResultValue()
                => CBCharacteristic.Value.ToArray();
        }

        class WriteCharacteristicOperation : CharacteristicOperation<byte[]>
        {
            private readonly byte[] _data;
            private bool _response;

            public WriteCharacteristicOperation(Characteristic characteristic, byte[] data, bool withoutResponse)
                : base(characteristic)
            {
                _data = data;
                _response = !withoutResponse;
            }

            protected override void Start()
            {
                RequireConnected();

                CBCharacteristicWriteType type = CBCharacteristicWriteType.WithResponse;

                if (_response)
                {
                    if (!Characteristic.CanWrite())
                    {
                        Logger.LogWarning("{Characteristic} cannot be written with response", Characteristic);
                    }
                }
                else if (Characteristic.CanWriteWithoutResponse())
                {
                    type = CBCharacteristicWriteType.WithoutResponse;
                }

                Logger.LogDebug("Writing {Characteristic} {Type}: {Data}", Characteristic, type, _data);

                CBPeripheral.WriteValue(NSData.FromArray(_data), CBCharacteristic, type);
                if (type == CBCharacteristicWriteType.WithoutResponse)
                {
                    if (CBPeripheral.CanSendWriteWithoutResponse)
                    {
                        Logger.LogDebug("CanSendWriteWithoutResponse = true, completing without waiting");
                        // another write can be queued, meaning there's no need to wait for the notification
                        SetResult(_data);
                    }
                    else
                    {
                        Logger.LogDebug("CanSendWriteWithoutResponse = false, waiting until ready");
                    }
                }
            }

            protected override byte[] GetResultValue() => _data;
        }

        class UpdateNotifyOperation : CharacteristicOperation<bool>
        {
            public UpdateNotifyOperation(Characteristic characteristic)
                : base(characteristic)
            {
            }

            public bool DesiredState { get; private set; }

            protected override void Start()
            {
                if (!Connection.IsActive)
                {
                    // nothing to do
                    SetResult(false);
                    return;
                }

                DesiredState = Characteristic.ShouldNotify;

                if (CBCharacteristic.IsNotifying == DesiredState)
                {
                    Logger.LogDebug("Notifications for {Characteristic} are already {NotifyState}", Characteristic, DesiredState ? "enabled" : "disabled");
                    SetResult(DesiredState);
                }

                Logger.LogDebug("{NotifyStateChange} notifications for {Characteristic}", DesiredState ? "Enabling" : "Disabling", Characteristic);
                CBPeripheral.SetNotifyValue(DesiredState, CBCharacteristic);
            }

            public override void Result(NSError error)
            {
                base.Result(error);

                if (DesiredState)
                {
                    if (error != null)
                    {
                        // notify existing observers that of the failure to subscrible
                        Characteristic.NotifyError(error.ToException());
                    }
                }
                else
                {
                    // notify existing observers that the notifications have ended
                    Characteristic.NotifyCompleted();
                }
            }

            protected override bool GetResultValue() => DesiredState;
        }

        class PeripheralLostOperation : Operation<IPeripheralConnection>
        {
            protected override void Start()
            {
                Connection.OnClosed(new BluetoothLEException("Peripheral lost"));
            }
        }
    }
}
