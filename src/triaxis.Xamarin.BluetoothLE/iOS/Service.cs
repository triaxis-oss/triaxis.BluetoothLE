using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    class Service : IService
    {
        PeripheralConnection _connection;
        CBService _service;
        readonly ServiceUuid _uuid;
        internal Task<IList<ICharacteristic>> _tCharacteristics;

        public Service(PeripheralConnection connection, CBService service)
        {
            _connection = connection;
            _service = service;
            _uuid = service.UUID.ToUuid();
        }

        public ref readonly ServiceUuid Uuid => ref _uuid;
        public PeripheralConnection Connection => _connection;
        public CBService CBService => _service;

        public Task<IList<ICharacteristic>> GetCharacteristicsAsync()
            => _connection.GetCharacteristicsAsync(this);
    }
}
