using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

namespace triaxis.BluetoothLE
{
    partial class Peripheral
    {
        /// <summary>
        /// Represents the lifetime of a single connection to a <see cref="Peripheral" />
        /// from connect to disconnect.
        /// Can be reused by mulitple <see cref="ConnectionInstance" />s
        /// </summary>
        class Connection
        {
            private readonly Peripheral _peripheral;
            private readonly int _num;
            /// <summary>
            /// List of all <see cref="ConnectionInstance"/>s actively using this connection.
            /// Items are present from creation until the instance is released/disconnected.
            /// </summary>
            private readonly List<ConnectionInstance> _instances;
            private Dictionary<ServiceUuid, GetServicesOperation> _serviceDiscoveryOperations;
            private GetServicesOperation _allServiceDiscoveryOperation;

            public Connection(Peripheral peripheral, int num)
            {
                _peripheral = peripheral;
                _num = num;
                _instances = new();
            }

            public Peripheral Peripheral => _peripheral;
            public CBPeripheral CBPeripheral => Peripheral.CBPeripheral;
            public bool IsActive => Peripheral._connection == this;
            public CBPeripheralState State => CBPeripheral.State;

            internal void OnCharacteristicValueChanged(CBCharacteristic characteristic, NSError error)
            {
                _instances.SafeForEach(inst => inst.OnCharacteristicValueChanged(characteristic, error));
            }

            internal void OnClosed(Exception error)
            {
                // connection is no longer reusable
                Interlocked.CompareExchange(ref _peripheral._connection, null, this);
                _instances.SafeForEachAndClear(inst => inst.OnClosed(error));
            }

            internal ConnectionInstance CreateInstance(int connectionTimeout)
            {
                var instance = new ConnectionInstance(this, connectionTimeout);
                _instances.Add(instance);
                return instance;
            }

            internal void RemoveInstance(ConnectionInstance instance)
            {
                _instances.Remove(instance);
                if (_instances.Count == 0)
                {
                    // last instance closed, request underlying connection disconnect
                    if (Interlocked.CompareExchange(ref _peripheral._connection, null, this) == this)
                    {
                        try
                        {
                            Peripheral.CBDisconnect();
                        }
                        catch (Exception e)
                        {
                            Peripheral._logger.LogError(e, "Error while disconnecting");
                        }
                    }
                }
            }

            internal Task GetServicesAsync(ConnectionInstance connectionInstance, ServiceUuid[] hint)
            {
                if (_allServiceDiscoveryOperation != null)
                {
                    // an operation to discover all services is already scheduled, just wait...
                    return _allServiceDiscoveryOperation.Task;
                }

                if (!(hint?.Length > 0))
                {
                    // we need to discover all services
                    return connectionInstance.Enqueue(_allServiceDiscoveryOperation = new GetServicesOperation());
                }

                // enqueue an operation to discover the services
                var op = new GetServicesOperation(hint);
                bool required = false;
                _serviceDiscoveryOperations ??= new();
                foreach (var uuid in hint)
                {
                    if (_serviceDiscoveryOperations.TryAdd(uuid, op))
                    {
                        required = true;
                    }
                }

                // we may have operations for all requested services already scheduled - if so, just wait for them to complete
                if (required)
                {
                    // schedule the discovery operation
                    return connectionInstance.Enqueue(op);
                }
                else
                {
                    // wait for all partial discovery operations to finish
                    return Task.WhenAll(hint.Select(uuid => _serviceDiscoveryOperations[uuid].Task).Distinct());
                }
            }
        }
    }
}
