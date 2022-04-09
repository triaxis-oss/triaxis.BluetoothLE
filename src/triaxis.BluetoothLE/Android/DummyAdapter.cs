using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace triaxis.BluetoothLE
{
    class DummyAdapter : IAdapter
    {
        public DummyAdapter(AdapterState state)
        {
            State = state;
        }

        public AdapterState State { get; private set; }

        public IObservable<IAdvertisement> Scan()
        {
            throw new NotSupportedException();
        }

        public IObservable<IAdvertisement> Scan(params ServiceUuid[] services)
        {
            throw new NotSupportedException();
        }
    }
}
