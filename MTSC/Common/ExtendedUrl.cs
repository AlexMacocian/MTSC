using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MTSC.Common
{
    internal sealed class ExtendedUrl
    {
        internal sealed class UrlComponent
        {
            public string Content { get; set; }
            public bool IsPlaceholder { get; set; }
        }

        private readonly List<UrlComponent> components = new();

        public string Url { get; }

        public ExtendedUrl(string url)
        {
            this.Url = url;
            this.ParseUrl();
        }

        public bool TryMatchUrl(string url, out List<UrlValue> urlValues)
        {
            urlValues = null;
            try
            {
                var matchingUrlComponents = SplitIntoComponents(url).ToList();
                var values = new List<UrlValue>();
                if (matchingUrlComponents.Count != this.components.Count)
                {
                    return false;
                }

                for(var i = 0; i < matchingUrlComponents.Count; i++)
                {
                    var matchingComponent = matchingUrlComponents[i];
                    var component = this.components[i];
                    if (component.IsPlaceholder)
                    {
                        values.Add(new UrlValue { Placeholder = component.Content.TrimStart('{').TrimEnd('}'), Value = matchingComponent.Content });
                    }
                    else if (component.Content != matchingComponent.Content)
                    {
                        return false;
                    }
                }

                urlValues = values;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ParseUrl()
        {
            this.components.AddRange(SplitIntoComponents(this.Url));
            foreach (var component in this.components)
            {
                if (component.Content.First() == '{' && component.Content.Last() == '}')
                {
                    component.IsPlaceholder = true;
                }
            }
        }

        private static IEnumerable<UrlComponent> SplitIntoComponents(string url)
        {
            return url.Split('/').Where(token => string.IsNullOrWhiteSpace(token) is false).Select(token => new UrlComponent { Content = token });
        }
    }
}
