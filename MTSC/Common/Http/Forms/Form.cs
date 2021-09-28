using System.Collections;
using System.Collections.Generic;

namespace MTSC.Common.Http.Forms
{
    public class Form : IEnumerable<KeyValuePair<string, ContentTypeBase>>
    {
        private Dictionary<string, ContentTypeBase> dictionary = new();

        public int Count { get => this.dictionary.Count; }

        public void SetValue(string key, ContentTypeBase value)
        {
            this.dictionary[key] = value;
        }

        public T GetValue<T>(string key) where T : ContentTypeBase
        {
            return this.dictionary[key] as T;
        }

        public bool TryGetValue<T>(string key, out T value) where T : ContentTypeBase
        {
            if (this.dictionary.TryGetValue(key, out var formValue))
            {
                if (formValue.GetType() == typeof(T))
                {
                    value = formValue as T;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public ContentTypeBase GetValue(string key)
        {
            return this.dictionary[key];
        }

        public IEnumerator<KeyValuePair<string, ContentTypeBase>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, ContentTypeBase>>)this.dictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, ContentTypeBase>>)this.dictionary).GetEnumerator();
        }
    }
}
