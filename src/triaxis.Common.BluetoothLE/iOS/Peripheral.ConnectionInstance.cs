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
        class ConnectionInstance : IPeripheralConnection
        {
            private readonly Connection _connection;
            private readonly Task<IPeripheralConnection> _tConnect;
            private readonly ConnectOperation _opConnect;
            private Task _tDisconnect;
            private Dictionary<CBService, Service> _services;
            private Dictionary<CBCharacteristic, Characteristic> _characteristics;
            private bool _closed;

            public ConnectionInstance(Connection connection, int timeout)
            {
                _connection = connection;
                _opConnect = new ConnectOperation();
                _tConnect = Enqueue(_opConnect, timeout);
                _services = new();
                _characteristics = new();
            }

            public Connection Connection => _connection;
            public Peripheral Peripheral => _connection.Peripheral;

            internal Task<T> Enqueue<T>(Operation<T> op, int timeout = -1)
            {
                op.Bind(this);
                return Peripheral.Enqueue(op, timeout);
            }

            public event EventHandler<Exception> Closed;

            internal Task<IPeripheralConnection> ConnectTask => _tConnect;

            public Task DisconnectAsync()
            {
                _opConnect.SetCanceled();
                return _tDisconnect ??= Enqueue(new DisconnectOperation());
            }

            internal void OnClosed(Exception error)
            {
                if (!_closed)
                {
                    _closed = true;
                    Connection.RemoveInstance(this);
                    _characteristics.SafeForEachAndClear(kvp => kvp.Value.NotifyCompleted());
                    try
                    {
                        Closed?.Invoke(this, error);
                    }
                    catch (Exception e)
                    {
                        Peripheral._logger.LogError(e, "Error calling Closed handler");
                    }
                }
            }

            ValueTask IAsyncDisposable.DisposeAsync()
                => new ValueTask(DisconnectAsync());

            public Task<string> GetDeviceNameAsync()
                => Task.FromResult(Peripheral.CBPeripheral.Name);

            public Task<IList<IService>> GetServicesAsync()
                => GetServicesAsync(null);

            private Service GetBoundService(CBService service)
                => _services.TryGetValue(service, out var svc) ? svc : _services[service] = new Service(this, service);

            public async Task<IList<IService>> GetServicesAsync(ServiceUuid[] hint)
            {
                await _connection.GetServicesAsync(this, hint);
                return Array.ConvertAll(Peripheral.CBPeripheral.Services, GetBoundService);
            }

            internal Task<IList<ICharacteristic>> GetCharacteristicsAsync(Service service)
                => Enqueue(new GetCharacteristicsOperation(service));

            internal Task<byte[]> ReadCharacteristicAsync(Characteristic characteristic)
                => Enqueue(new ReadCharacteristicOperation(characteristic));

            internal Task WriteCharacteristicAsync(Characteristic characteristic, byte[] data, bool withoutResponse)
                => Enqueue(new WriteCharacteristicOperation(characteristic, data, withoutResponse));

            internal void UpdateNotifications(Characteristic characteristic)
                => Enqueue(new UpdateNotifyOperation(characteristic));

            public Task IdleAsync()
                => Peripheral._q.IdleAsync();

            public Task<int> RequestMaximumWriteAsync(int request)
            {
                int max = (int)Peripheral.CBPeripheral.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse);
                return Task.FromResult(Math.Min(request, max));
            }

            internal Characteristic CreateCharacteristic(Service service, CBCharacteristic ch)
            {
                var res = new Characteristic(service, ch);
                _characteristics.Add(ch, res);
                return res;
            }

            internal void OnCharacteristicValueChanged(CBCharacteristic characteristic, NSError error)
            {
                if (_characteristics.TryGetValue(characteristic, out var ch))
                {
                    ch.NotifyNext(characteristic.Value.ToArray());
                }
            }
        }
    }
}
