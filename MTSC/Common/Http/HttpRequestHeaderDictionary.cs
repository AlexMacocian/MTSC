﻿using System.Collections;
using System.Collections.Generic;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http
{
    /// <summary>
    /// Dictionary of http request headers.
    /// </summary>
    public sealed class HttpRequestHeaderDictionary : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> headers { get; } = new Dictionary<string, string>();

        public string this[string key] { get => headers[key]; set => headers[key] = value; }
        public string this[GeneralHeaders key] { get => headers[HttpHeaders.GeneralHeaders[(int)key]]; set => headers[HttpHeaders.GeneralHeaders[(int)key]] = value; }
        public string this[EntityHeaders key] { get => headers[HttpHeaders.EntityHeaders[(int)key]]; set => headers[HttpHeaders.EntityHeaders[(int)key]] = value; }
        public string this[RequestHeaders key] { get => headers[HttpHeaders.RequestHeaders[(int)key]]; set => headers[HttpHeaders.RequestHeaders[(int)key]] = value; }

        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(RequestHeaders header)
        {
            return ContainsHeader(HttpHeaders.RequestHeaders[(int)header]);
        }
        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(GeneralHeaders header)
        {
            return ContainsHeader(HttpHeaders.GeneralHeaders[(int)header]);
        }
        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(EntityHeaders header)
        {
            return ContainsHeader(HttpHeaders.EntityHeaders[(int)header]);
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
        /// <summary>
        /// Adds header with specified value.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="value"></param>
        /// <returns>This dictionary object.</returns>
        public HttpRequestHeaderDictionary AddHeader(RequestHeaders header, string value)
        {
            this[header] = value;
            return this;
        }
        /// <summary>
        /// Adds header with specified value.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="value"></param>
        /// <returns>This dictionary object.</returns>
        public HttpRequestHeaderDictionary AddHeader(GeneralHeaders header, string value)
        {
            this[header] = value;
            return this;
        }
        /// <summary>
        /// Adds header with specified value.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="value"></param>
        /// <returns>This dictionary object.</returns>
        public HttpRequestHeaderDictionary AddHeader(EntityHeaders header, string value)
        {
            this[header] = value;
            return this;
        }
        /// <summary>
        /// Adds header with specified value.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="value"></param>
        /// <returns>This dictionary object.</returns>
        public HttpRequestHeaderDictionary AddHeader(string header, string value)
        {
            this[header] = value;
            return this;
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
