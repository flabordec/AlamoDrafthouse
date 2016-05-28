using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace com.magusoft.drafthouse.ExtensionMethods
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
	}
}
