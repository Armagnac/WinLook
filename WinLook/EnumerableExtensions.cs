using System;
using System.Collections.Generic;
using System.Linq;

namespace WinLook
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<KeyValuePair<TKey, TValue>> Except<TKey, TValue>(this Dictionary<TKey, TValue> source, IEnumerable<TKey> exceptSource)
        {
            return source.Keys.Except(exceptSource).Select(key => new KeyValuePair<TKey, TValue>(key, source[key]));
        }

        /// <summary>
        /// The same as Distinct only it receives a selector function instead of a comparer (thus easier to use).
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, TValue> selector)
        {
            var comparer = DistinctByComparer<TSource>.CompareBy(
                selector, EqualityComparer<TValue>.Default);
            return new HashSet<TSource>(source, comparer);
        }

        private static class DistinctByComparer<TSource>
        {
            public static IEqualityComparer<TSource> CompareBy<TValue>(Func<TSource, TValue> selector)
            {
                return CompareBy(selector, EqualityComparer<TValue>.Default);
            }
            public static IEqualityComparer<TSource> CompareBy<TValue>(
                Func<TSource, TValue> selector,
                IEqualityComparer<TValue> comparer)
            {
                return new ComparerImpl<TValue>(selector, comparer);
            }
            sealed class ComparerImpl<TValue> : IEqualityComparer<TSource>
            {
                private readonly Func<TSource, TValue> _Selector;
                private readonly IEqualityComparer<TValue> _Comparer;
                public ComparerImpl(
                    Func<TSource, TValue> selector,
                    IEqualityComparer<TValue> comparer)
                {
                    if (selector == null)
                        throw new ArgumentNullException(nameof(selector));
                    if (comparer == null)
                        throw new ArgumentNullException(nameof(comparer));
                    this._Selector = selector;
                    this._Comparer = comparer;
                }

                Boolean IEqualityComparer<TSource>.Equals(TSource x, TSource y)
                {
                    if (x == null && y == null)
                        return true;
                    if (x == null || y == null)
                        return false;
                    return _Comparer.Equals(_Selector(x), _Selector(y));
                }

                Int32 IEqualityComparer<TSource>.GetHashCode(TSource obj)
                {
                    return obj == null ? 0 : _Comparer.GetHashCode(_Selector(obj));
                }
            }
        }
    }
}
