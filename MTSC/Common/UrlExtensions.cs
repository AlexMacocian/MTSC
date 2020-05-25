using System.Collections.Generic;
using System.Text;

namespace MTSC.Common
{
    static class UrlExtensions
    {
        public static IReadOnlyList<UrlPlaceholder> ExtractUrlPlaceholders(string url)
        {
            var list = new List<UrlPlaceholder>();
            bool begin = false;
            StringBuilder tokenBuilder = new StringBuilder();
            var index = 0;
            for (int i = 0; i < url.Length; i++)
            {
                var c = url[i];
                if (c == '{')
                {
                    begin = true;
                    tokenBuilder.Clear();
                    continue;
                }
                else if (c == '}')
                {
                    begin = false;
                    list.Add(new UrlPlaceholder { Index = index, Placeholder = tokenBuilder.ToString(), ContinuationChar = i + 1 < url.Length ? url[i + 1] : (char)0 });
                    continue;
                }
                if (begin)
                {
                    tokenBuilder.Append(c);
                }
                else
                {
                    index++;
                }
            }
            return list;
        }

        public static IReadOnlyList<UrlValue> ExtractValuesFromPlaceholders(string url, IReadOnlyList<UrlPlaceholder> placeholders)
        {
            var list = new List<UrlValue>();
            if (placeholders.Count == 0)
            {
                return list;
            }
            StringBuilder valueBuilder = new StringBuilder();
            var placeHolderIndex = 0;
            var otherCharacterIndex = 0;
            for (int i = 0; i < url.Length; i++)
            {
                while (otherCharacterIndex < placeholders[placeHolderIndex].Index)
                {
                    i++;
                    otherCharacterIndex++;
                }
                while (i < url.Length && url[i] != placeholders[placeHolderIndex].ContinuationChar)
                {
                    var c = url[i];
                    valueBuilder.Append(c);
                    i++;
                }
                list.Add(new UrlValue { Placeholder = placeholders[placeHolderIndex].Placeholder, Value = valueBuilder.ToString() });
                placeHolderIndex++;
                valueBuilder.Clear();
            }
            return list;
        }
    }
}
