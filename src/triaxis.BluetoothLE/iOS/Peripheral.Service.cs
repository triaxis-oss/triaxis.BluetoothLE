using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace triaxis.BluetoothLE
{
    partial class Peripheral
    {
        class Service : IService
        {
            ConnectionInstance _connection;
            CBService _service;
            readonly ServiceUuid _uuid;
            internal Task<IList<ICharacteristic>> _tCharacteristics;

            public Service(ConnectionInstance connection, CBService service)
            {
                _connection = connection;
                _service = service;
                _uuid = service.UUID.ToUuid();
            }

            public ref readonly ServiceUuid Uuid => ref _uuid;
            public ConnectionInstance Connection => _connection;
            public CBService CBService => _service;

            public Task<IList<ICharacteristic>> GetCharacteristicsAsync()
                => _tCharacteristics ??= _connection.GetCharacteristicsAsync(this);

            public override string ToString()
                => _uuid.ToString();
        }
    }
}
