using System.Collections;
using System.Collections.Generic;

namespace MTSC.Common.Http.Forms
{
    public class Form : IEnumerable<KeyValuePair<string, ContentTypeBase>>
    {
        private Dictionary<string, ContentTypeBase> dictionary = new Dictionary<string, ContentTypeBase>();

        public int Count { get => dictionary.Count; }

        public void SetValue(string key, ContentTypeBase value)
        {
            dictionary[key] = value;
        }

        public T GetValue<T>(string key) where T : ContentTypeBase
        {
            return dictionary[key] as T;
        }

        public ContentTypeBase GetValue(string key)
        {
            return dictionary[key];
        }

        public IEnumerator<KeyValuePair<string, ContentTypeBase>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, ContentTypeBase>>)dictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, ContentTypeBase>>)dictionary).GetEnumerator();
        }
    }
}
