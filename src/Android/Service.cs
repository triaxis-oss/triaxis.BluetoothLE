using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace triaxis.Xamarin.BluetoothLE.Android
{
    class Service : IService
    {
        PeripheralConnection _connection;
        BluetoothGattService _service;
        Guid _uuid;
        IList<ICharacteristic> _characteristics;

        public Service(PeripheralConnection connection, BluetoothGattService service)
        {
            _connection = connection;
            _service = service;
            _uuid = new Guid(service.Uuid.ToString());
            _characteristics = _service.Characteristics.SelectArray(ch => new Characteristic(this, ch));
        }

        public Guid Uuid => _uuid;
        public PeripheralConnection Connection => _connection;
        public BluetoothGattService SystemService => _service;

        public Task<IList<ICharacteristic>> GetCharacteristicsAsync()
            => Task.FromResult(_characteristics);
    }
}
