using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using CoreBluetooth;
using Foundation;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(triaxis.Xamarin.BluetoothLE.iOS.Platform))]

namespace triaxis.Xamarin.BluetoothLE.iOS
{
    class Platform : IBluetoothLE
    {
        ReplaySubject<IAdapter> _adapterSubject;

        [Preserve]
        public Platform() { }

        public IObservable<IAdapter> WhenAdapterChanges()
            => _adapterSubject ?? (_adapterSubject = Init());

        ReplaySubject<IAdapter> Init()
        {
            var subj = new ReplaySubject<IAdapter>(1);
            subj.OnNext(new Adapter(this));
            return subj;
        }

        internal void FireAdapterChange(Adapter next)
            => _adapterSubject.OnNext(next);
    }
}
