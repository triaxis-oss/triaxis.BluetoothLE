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

namespace triaxis.BluetoothLE
{
    class Service : IService
    {
        PeripheralConnection _connection;
        BluetoothGattService _service;
        readonly ServiceUuid _uuid;
        IList<ICharacteristic> _characteristics;

        public Service(PeripheralConnection connection, BluetoothGattService service)
        {
            _connection = connection;
            _service = service;
            _uuid = new ServiceUuid(service.Uuid.ToUuid());
            _characteristics = _service.Characteristics.SelectArray(ch => new Characteristic(this, ch));
        }

        public ref readonly ServiceUuid Uuid => ref _uuid;
        public PeripheralConnection Connection => _connection;
        public BluetoothGattService SystemService => _service;

        public Task<IList<ICharacteristic>> GetCharacteristicsAsync()
            => Task.FromResult(_characteristics);
    }
}
