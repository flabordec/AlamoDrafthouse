using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MaguSoft.ComeAndTicket.Core.ExtensionMethods
{
    static class HtmlNodeExtensionMethods
    {
        public static bool AttributeExistsAndHasValue(this HtmlNode node, string attributeName, params string[] expectedValues)
        {
            string attributeValue = node.Attributes[attributeName]?.Value;
            if (attributeValue != null)
            {
                foreach (string expectedValue in expectedValues)
                {
                    if (!attributeValue.Contains(expectedValue))
                        return false;
                }
                return true;
            }

            return false;
        }

        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                collection.Add(value);
            }
        }
    }

    public static class EnumerableExtensionMethods
    {
        public static Dictionary<TKey, List<TSource>> ToDictionaryList<TSource, TKey>(
            this IEnumerable<TSource> source, 
            Func<TSource, TKey> keySelector, 
            IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source == null)
            {
                throw new NullReferenceException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new NullReferenceException(nameof(keySelector));
            }

            int capacity = 0;
            if (source is ICollection<TSource> collection)
            {
                capacity = collection.Count;
                if (capacity == 0)
                {
                    return new Dictionary<TKey, List<TSource>>(comparer);
                }
            }

            var d = new Dictionary<TKey, List<TSource>>(capacity, comparer);
            foreach (TSource element in source)
            {
                TKey key = keySelector(element);
                if (!d.ContainsKey(key))
                    d.Add(key, new List<TSource>());
                d[key].Add(element);
            }

            return d;
        }
    }
}
