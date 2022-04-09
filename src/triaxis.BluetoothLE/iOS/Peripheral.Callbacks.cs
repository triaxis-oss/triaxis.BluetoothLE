using System;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

namespace triaxis.BluetoothLE
{
    partial class Peripheral : CBPeripheralDelegate
    {
        #region Internal (connection-related) callbacks

        internal void OnConnected()
        {
            if (_q.TryGetCurrent<ConnectOperation>(out var op) && !op.Task.IsCompleted)
            {
                op.SetResult(op.Owner);
            }
            else
            {
                _logger.LogWarning("received unexpected Connected notification");
            }
        }

        internal void OnConnectionFailed(NSError error)
        {
            if (_q.TryGetCurrent<ConnectOperation>(out var op))
            {
                if (!op.CheckSuccess(error))
                {
                    op.SetException(new BluetoothLEException("Unknown error occured while connecting"));
                }
            }
            else
            {
                _logger.LogWarning("received unexpected ConnectionFailed notification: {Error}", error);
            }
        }

        internal void OnDisconnected(NSError error)
        {
            if (_q.TryGetCurrent<IPeripheralOperation>(out var op) && !op.Task.IsCompleted)
            {
                _logger.LogDebug("Disconnected during {Operation}: {Error}", op, error);
                op.HandleDisconnect(error);
            }
            else if (_connection is Connection con)
            {
                _logger.LogDebug("Disconnected with no pending operation: {Error}", error);
                con.OnClosed(error.ToException());
            }
            else
            {
                _logger.LogWarning("received unexpected Disconnected notification: {Error}", error);
            }
        }

        #endregion

        #region Delegate callbacks

        public override void DiscoveredService(CBPeripheral peripheral, NSError error)
        {
            System.Diagnostics.Debug.Assert(peripheral == _cbPeripheral);

            _logger.LogDebug("DiscoveredServices callback: {Error}", error);

            if (_q.TryGetCurrent<GetServicesOperation>(out var op))
            {
                if (op.CheckSuccess(error))
                {
                    op.DidDiscoverServices();
                }
            }
            else
            {
                _logger.LogWarning("Unexpected DiscoveredServices callback");
            }
        }

#if XAMARIN
        public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError error)
#else
        public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError error)
#endif
        {
            _logger.LogDebug("DiscoveredCharacteristics callback: {Error}", error);

            if (_q.TryGetCurrent<GetCharacteristicsOperation>(out var op) && op.Service.CBService == service)
            {
                if (op.CheckSuccess(error))
                {
                    op.DidDiscoverCharacteristics();
                }
            }
            else
            {
                _logger.LogWarning("Unexpected DiscoveredCharacteristics callback");
            }
        }

        public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
        {
            _logger.LogTrace("UpdatedCharacteristicValue callback: {Characteristic} {Error}", characteristic, error);

            if (_q.TryGetCurrent<ReadCharacteristicOperation>(out var op) && op.CBCharacteristic == characteristic)
            {
                op.Result(error);
            }
            else if (_connection is Connection con)
            {
                con.OnCharacteristicValueChanged(characteristic, error);
            }
        }

        public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
        {
            _logger.LogDebug("WroteCharacteristicValue callback: {Characteristic} {Error}", characteristic, error);

            if (_q.TryGetCurrent<WriteCharacteristicOperation>(out var op) && op.CBCharacteristic == characteristic)
            {
                op.Result(error);
            }
        }

        public override void IsReadyToSendWriteWithoutResponse(CBPeripheral peripheral)
        {
            _logger.LogDebug("IsReadyToSendWriteWithoutResponse callback");
            
            if (_q.TryGetCurrent<WriteCharacteristicOperation>(out var op))
            {
                op.Result(null);
            }
        }

        public override void UpdatedNotificationState(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
        {
            _logger.LogDebug("UpdatedNotificationState callback: {Characteristic} {Error}", characteristic, error);

            if (_q.TryGetCurrent<UpdateNotifyOperation>(out var op))
            {
                op.Result(error);
            }
        }

        #endregion
    }
}
