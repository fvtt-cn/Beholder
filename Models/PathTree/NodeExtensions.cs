using System;
using System.Collections.Generic;
using System.Linq;

namespace Beholder.Models.PathTree
{
    public static class NodeExtensions
    {
        public static IEnumerable<T> Values<T>(this IEnumerable<Node<T>> nodes)
        {
            return nodes.Select(n => n.Value);
        }

        public static IEnumerable<TSource> Duplicates<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector)
        {
            var grouped = source.GroupBy(selector);
            var moreThen1 = grouped.Where(i => i.IsMultiple());

            return moreThen1.SelectMany(i => i);
        }

        public static bool IsMultiple<T>(this IEnumerable<T> source)
        {
            using var enumerator = source.GetEnumerator();
            return enumerator.MoveNext() && enumerator.MoveNext();
        }

        public static IEnumerable<T> ToIEnumerable<T>(this T item)
        {
            yield return item;
        }
    }
}
