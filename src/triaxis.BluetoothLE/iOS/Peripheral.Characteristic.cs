using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace triaxis.BluetoothLE
{
    partial class Peripheral
    {
        class Characteristic : ICharacteristic
        {
            private readonly Service _service;
            private readonly CBCharacteristic _characteristic;
            private readonly CharacteristicUuid _uuid;
            private readonly List<IObserver<byte[]>> _observers;

            public Characteristic(Service service, CBCharacteristic characteristic)
            {
                _service = service;
                _characteristic = characteristic;
                _uuid = characteristic.UUID.ToUuid();
                _observers = new();
            }

            public ref readonly CharacteristicUuid Uuid => ref _uuid;
            public CharacteristicProperties Properties => (CharacteristicProperties)_characteristic.Properties;
            public CBCharacteristic CBCharacteristic => _characteristic;

            public bool ShouldNotify => _observers.Count > 0;

            public IObservable<byte[]> Notifications() => Observable.Create<byte[]>(observer =>
            {
                _observers.Add(observer);
                _service.Connection.UpdateNotifications(this);

                return () =>
                {
                    if (_observers.Remove(observer))
                    {
                        _service.Connection.UpdateNotifications(this);
                    }
                };
            });

            internal void NotifyNext(byte[] val)
            {
                _observers.SafeForEach(o => o.OnNext(val));
            }

            internal void NotifyCompleted()
            {
                _observers.SafeForEachAndClear(o => o.OnCompleted());
            }

            internal void NotifyError(Exception err)
            {
                _observers.SafeForEachAndClear(o => o.OnError(err));
            }

            public Task<byte[]> ReadAsync()
               => _service.Connection.ReadCharacteristicAsync(this);

            public Task WriteAsync(byte[] data)
                => _service.Connection.WriteCharacteristicAsync(this, data, false);

            public Task WriteWithoutResponseAsync(byte[] data)
                => _service.Connection.WriteCharacteristicAsync(this, data, true);

            public override string ToString()
                => _uuid.ToString();
        }
    }
}
