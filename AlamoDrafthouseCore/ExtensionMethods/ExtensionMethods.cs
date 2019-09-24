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
}
