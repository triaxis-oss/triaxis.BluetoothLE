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
    }
}
