using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace triaxis.Xamarin.BluetoothLE.Android
{
    static class PrivateExtensions
    {
        public static TOutput[] SelectArray<TInput, TOutput>(this IList<TInput> list, Converter<TInput, TOutput> conversion)
        {
            if (list == null)
                return null;
            var res = new TOutput[list.Count];
            for (int i = 0; i < res.Length; i++)
                res[i] = conversion(list[i]);
            return res;
        }

        public static Uuid ToUuid(this Java.Util.UUID uuid)
        {
            return new Uuid((ulong)uuid.MostSignificantBits, (ulong)uuid.LeastSignificantBits);
        }

        public static Java.Util.UUID ToJavaUuid(this in Uuid uuid)
        {
            return new Java.Util.UUID((long)uuid.LeftHalf, (long)uuid.RightHalf);
        }

        public static ParcelUuid ToParcelUuid(this in Uuid uuid)
        {
            return new ParcelUuid(uuid.ToJavaUuid());
        }
    }
}
