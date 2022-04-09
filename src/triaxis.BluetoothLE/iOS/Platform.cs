using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace triaxis.BluetoothLE
{
    /// <summary>
    /// Platform-specific implementation of the <see cref="IBluetoothLE"/> interface for iOS
    /// </summary>
    public class Platform : IBluetoothLE
    {
        private readonly ILoggerFactory _loggerFactory;
        ReplaySubject<IAdapter> _adapterSubject;

        /// <summary>
        /// Creates an instance of the platform-specific <see cref="IBluetoothLE"/> implementation
        /// </summary>
        [Preserve]
        public Platform(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Gets an observable returning values when Bluetooth LE adapter availability changes
        /// </summary>
        public IObservable<IAdapter> WhenAdapterChanges()
            => _adapterSubject ?? (_adapterSubject = Init());

        ReplaySubject<IAdapter> Init()
        {
            var subj = new ReplaySubject<IAdapter>(1);
            subj.OnNext(new Adapter(this, _loggerFactory));
            return subj;
        }

        internal void FireAdapterChange(Adapter next)
            => _adapterSubject.OnNext(next);
    }
}
