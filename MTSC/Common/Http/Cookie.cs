using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common.Http
{
    /// <summary>
    /// Class for HTTP cookies.
    /// </summary>
    public sealed class Cookie
    {
        /// <summary>
        /// Key of cookie.
        /// </summary>
        public string Key { get; private set; }
        /// <summary>
        /// Value of cookie.
        /// </summary>
        public string Value { get; private set; }
        /// <summary>
        /// Dictionary containing cookie attributes.
        /// </summary>
        public Dictionary<string, string> Attributes { get; private set; }
        /// <summary>
        /// Creates a new instance of cookie class.
        /// </summary>
        /// <param name="key">Key of cookie.</param>
        /// <param name="value">Value of cookie.</param>
        public Cookie(string key, string value)
        {
            Attributes = new Dictionary<string, string>();
            this.Key = key;
            this.Value = value;
        }
        /// <summary>
        /// Creates a new instance of cookie class.
        /// </summary>
        /// <param name="cookieString">String containing the definition of a cookie</param>
        public Cookie(string cookieString)
        {
            Attributes = new Dictionary<string, string>();
            string[] cookieTokens = cookieString.Split(';');
            Key = cookieTokens[0].Split('=')[0].Trim();
            Value = cookieTokens[1].Split('=')[1].Trim();
            for(int i = 1; i < cookieTokens.Length; i++)
            {
                string[] attributeTokens = cookieTokens[i].Split('=');
                Attributes[attributeTokens[0].Trim()] = attributeTokens.Length > 1 ? attributeTokens[1].Trim() : string.Empty;
            }
        }
        /// <summary>
        /// Build the string containing the cookie definition.
        /// </summary>
        /// <returns>String containing the cookie definition.</returns>
        public string BuildCookieString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Key).Append('=').Append(Value);
            if(Attributes.Count > 0)
            {
                foreach(KeyValuePair<string, string> attribute in Attributes)
                {
                    sb.Append(';').Append(attribute.Key);
                    if (!string.IsNullOrWhiteSpace(attribute.Value))
                    {
                        sb.Append('=').Append(attribute.Value);
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Build the byte array containing the cookie definition.
        /// </summary>
        /// <returns>Byte array containin the cookie definition.</returns>
        public byte[] BuildCookieBytes()
        {
            return ASCIIEncoding.ASCII.GetBytes(BuildCookieString());
        }
    }
}
