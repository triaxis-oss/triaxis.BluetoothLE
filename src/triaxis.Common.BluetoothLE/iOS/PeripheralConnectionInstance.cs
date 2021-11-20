using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if XAMARIN
namespace triaxis.Xamarin.BluetoothLE.iOS
#else
namespace triaxis.Maui.BluetoothLE.iOS
#endif
{
    class PeripheralConnectionInstance : IPeripheralConnection
    {
        private readonly PeripheralConnection _connection;
        private int _closed;

        public PeripheralConnectionInstance(PeripheralConnection connection)
        {
            _connection = connection;
        }

        public event EventHandler<Exception> Closed
        {
            add => _connection.Closed += value;
            remove => _connection.Closed -= value;
        }

        public async Task<IPeripheralConnection> ConnectAsync(int timeout)
        {
            await _connection.ConnectAsync(timeout);
            return this;
        }

        public Task DisconnectAsync()
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0)
            {
                throw new InvalidOperationException("Connection is already closed");
            }

            return _connection.DisconnectAsync();
        }

        ValueTask IAsyncDisposable.DisposeAsync()
            => new ValueTask(DisconnectAsync());

        public Task<string> GetDeviceNameAsync()
            => _connection.GetDeviceNameAsync();

        public Task<IList<IService>> GetServicesAsync()
            => _connection.GetServicesAsync();

        public Task<IList<IService>> GetServicesAsync(params ServiceUuid[] hint)
            => _connection.GetServicesAsync(hint);

        public Task IdleAsync()
            => _connection.IdleAsync();

        public Task<int> RequestMaximumWriteAsync(int request)
            => _connection.RequestMaximumWriteAsync(request);
    }
}
