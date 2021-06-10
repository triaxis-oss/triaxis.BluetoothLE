using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
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

namespace triaxis.Xamarin.BluetoothLE.Android
{
    class Characteristic : ICharacteristic
    {
        Service _service;
        BluetoothGattCharacteristic _characteristic;
        BluetoothGattDescriptor _clientConfigDescriptor;
        readonly CharacteristicUuid _uuid;

        static readonly Java.Util.UUID s_uuidClientConfiguration =
            Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");

        public Characteristic(Service service, BluetoothGattCharacteristic characteristic)
        {
            _service = service;
            _characteristic = characteristic;
            _uuid = new CharacteristicUuid(new Uuid(characteristic.Uuid.ToString()));
            _clientConfigDescriptor = characteristic.GetDescriptor(s_uuidClientConfiguration);
        }

        public ref readonly CharacteristicUuid Uuid => ref _uuid;
        public CharacteristicProperties Properties => (CharacteristicProperties)_characteristic.Properties;
        public Service Service => _service;
        public BluetoothGattCharacteristic SystemCharacteristic => _characteristic;
        public BluetoothGattDescriptor ClientConfigDescriptor => _clientConfigDescriptor;

        public IObservable<byte[]> Notifications() => Observable.Create<byte[]>(async (observer) =>
            {
                bool connected = true;

                void HandleData(BluetoothGattCharacteristic ch, byte[] val)
                {
                    if (ch == _characteristic)
                        observer.OnNext(val);
                };

                void HandleClosed(object sender, Exception err)
                {
                    connected = false;

                    if (err == null)
                        observer.OnCompleted();
                    else
                        observer.OnError(err);
                }

                _service.Connection.CharacteristicChanged += HandleData;
                _service.Connection.Closed += HandleClosed;

                await _service.Connection.EnableCharacteristicNotificationsAsync(this);

                return async () =>
                {
                    _service.Connection.Closed -= HandleClosed;

                    try
                    {
                        if (connected)
                        {
                            await _service.Connection.DisableCharacteristicNotificationsAsync(this);
                        }
                    }
                    catch (Exception e)
                    {
                        _service.Connection._logger.LogError(e, "Failed to disable characteristic notifications for {Characteristic}", _uuid);
                    }
                    finally
                    {
                        _service.Connection.CharacteristicChanged -= HandleData;
                    }
                };
            });

        public Task<byte[]> ReadAsync()
            => _service.Connection.ReadCharacteristicAsync(this);

        public Task WriteAsync(byte[] data)
            => _service.Connection.WriteCharacteristicAsync(this, data, false);

        public Task WriteWithoutResponseAsync(byte[] data)
            => _service.Connection.WriteCharacteristicAsync(this, data, true);
    }
}
