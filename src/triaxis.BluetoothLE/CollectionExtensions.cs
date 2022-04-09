using System;
using System.Collections.Generic;
using System.Linq;

namespace triaxis.BluetoothLE
{
    static class CollectionExtensions
    {
        public static void SafeForEachAndClear<T>(this ICollection<T> collection, Action<T> action)
        {
            var snap = collection.ToArray();
            collection.Clear();
            snap.SafeForEach(action);
        }

        public static void SafeForEach<T>(this ICollection<T> collection, Action<T> action)
        {
            Exception first = null;
            List<Exception> exceptions = null;

            foreach (var item in collection)
            {
                try
                {
                    action(item);
                }
                catch (Exception e)
                {
                    if (first == null)
                    {
                        first = e;
                    }
                    else
                    {
                        if (exceptions == null)
                        {
                            exceptions = new();
                            exceptions.Add(first);
                        }
                    }
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }

            if (first != null)
            {
                throw first;
            }
        }
    }
}
