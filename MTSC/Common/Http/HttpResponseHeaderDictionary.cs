using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http
{
    public class HttpResponseHeaderDictionary : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> headers { get; } = new Dictionary<string, string>();

        public string this[string key] { get => headers[key]; set => headers[key] = value; }
        public string this[GeneralHeadersEnum key] { get => headers[HttpHeaders.generalHeaders[(int)key]]; set => headers[HttpHeaders.generalHeaders[(int)key]] = value; }
        public string this[EntityHeadersEnum key] { get => headers[HttpHeaders.entityHeaders[(int)key]]; set => headers[HttpHeaders.entityHeaders[(int)key]] = value; }
        public string this[ResponseHeadersEnum key] { get => headers[HttpHeaders.responseHeaders[(int)key]]; set => headers[HttpHeaders.responseHeaders[(int)key]] = value; }

        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(ResponseHeadersEnum header)
        {
            return ContainsHeader(HttpHeaders.responseHeaders[(int)header]);
        }
        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(GeneralHeadersEnum header)
        {
            return ContainsHeader(HttpHeaders.generalHeaders[(int)header]);
        }
        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(EntityHeadersEnum header)
        {
            return ContainsHeader(HttpHeaders.entityHeaders[(int)header]);
        }
        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(string header)
        {
            return headers.ContainsKey(header);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string>>)headers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string>>)headers).GetEnumerator();
        }
    }
}
