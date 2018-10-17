using System;
using System.Collections;

namespace Tremblay.DatabaseUtilities.Sql.Extensions
{
    public static class EnumerableExtensions
    {

        public static int Count(this IEnumerable data)
        {
            if (data is ICollection list)
                return list.Count;
            
            var count = 0;
            var iter = data.GetEnumerator();
            using (iter as IDisposable)
                while (iter.MoveNext()) count++;
            
            return count;
        }

        public static object FirstOrDefault(this IEnumerable data, Func<object, bool> predicate = null)
        {
            if (data is IList list && list.Count > 0)
                return list[0];
            
            var iter = data.GetEnumerator();

            using (iter as IDisposable)
            {
                while (iter.MoveNext())
                {
                    if (predicate == null)
                        return iter.Current;

                    if (predicate.Invoke(iter.Current))
                        return iter.Current;

                }
            }

            return null;
        }

    }
}
