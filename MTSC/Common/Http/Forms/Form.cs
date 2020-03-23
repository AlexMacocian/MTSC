using System.Collections.Generic;

namespace MTSC.Common.Http.Forms
{
    public class Form
    {
        private Dictionary<string, ContentTypeBase> dictionary = new Dictionary<string, ContentTypeBase>();

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
    }
}
