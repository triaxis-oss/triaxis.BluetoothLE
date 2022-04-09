using System;

namespace triaxis.BluetoothLE
{
    static class ArrayExtensions
    {
        public static T[] Append<T>(this T[] array, T element)
        {
            var res = new T[array.Length + 1];
            array.CopyTo(res, 0);
            res[res.Length - 1] = element;
            return res;
        }

        public static T[] Remove<T>(this T[] array, T element)
        {
            int index = Array.IndexOf(array, element);
            if (index < 0)
                return array;
            if (array.Length == 1)
                return Array.Empty<T>();
            var res = new T[array.Length - 1];
            if (index > 0)
                Array.Copy(array, res, index);
            if (index < res.Length)
                Array.Copy(array, index + 1, res, index, res.Length - index);
            return res;
        }

        public static T[] Remove<T>(this T[] array, Predicate<T> predicate)
        {
            int index = Array.FindIndex(array, predicate);
            if (index < 0)
                return array;
            if (array.Length == 1)
                return Array.Empty<T>();
            var res = new T[array.Length - 1];
            if (index > 0)
                Array.Copy(array, res, index);
            if (index < res.Length)
                Array.Copy(array, index + 1, res, index, res.Length - index);
            return res;
        }
    }
}
