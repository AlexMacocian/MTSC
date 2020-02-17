﻿using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MTSC.Common.Http
{
    public class HttpMessage
    {
        public enum StatusCodes
        {
            Continue = 100,
            SwitchingProtocols = 101,
            OK = 200,
            Created = 201,
            Accepted = 202,
            NonAuthoritativeInformation = 203,
            NoContent = 204,
            ResetContent = 205,
            PartialContent = 206,
            MultipleChoices = 300,
            MovedPermanently = 301,
            Found = 302,
            SeeOther = 303,
            NotModified = 304,
            UseProxy = 305,
            TemporaryRedirect = 307,
            BadRequest = 400,
            Unauthorized = 401,
            PaymentRequired = 402,
            Forbidden = 403,
            NotFound = 404,
            MethodNotAllowed = 405,
            NotAcceptable = 406,
            ProxyAuthenticationRequired = 407,
            RequestTimeout = 408,
            Conflict = 409,
            Gone = 410,
            LengthRequired = 411,
            PreconditionFailed = 412,
            RequestEntityTooLarge = 413,
            RequestURITooLarge = 414,
            UnsupportedMediaType = 415,
            RequestRangeNotSatisfiable = 416,
            ExpectationFailed = 417,
            InternalServerError = 500,
            NotImplemented = 501,
            BadGateway = 502,
            ServiceUnavailable = 503,
            GatewayTimeout = 504,
            HTTPVersionNotSupported = 505
        }
        public enum HttpMethods
        {
            Options = 0,
            Get = 1,
            Head = 2,
            Post = 3,
            Put = 4,
            Delete = 5,
            Trace = 6,
            Connect = 7,
            ExtensionMethod = 8
        }
        public enum GeneralHeaders
        {
            CacheControl = 0,
            Connection = 1,
            Date = 2,
            Pragma = 3,
            Trailer = 4,
            TransferEncoding = 5,
            Upgrade = 6,
            Via = 7,
            Warning = 8
        }
        public enum RequestHeaders
        {
            Accept = 0,
            AcceptCharset = 1,
            AcceptEncoding = 2,
            AcceptLanguage = 3,
            Authorization = 4,
            Expect = 5,
            From = 6,
            Host = 7,
            IfMatch = 8,
            IfModifiedSince = 9,
            IfNoneMatch = 10,
            IfRange = 11,
            IfUnmodifiedSince = 12,
            MaxForwards = 13,
            ProxyAuthorization = 14,
            Range = 15,
            Referer = 16,
            TE = 17,
            UserAgent = 18
        }
        public enum ResponseHeaders
        {
            AcceptRanges = 0,
            Age = 1,
            ETag = 2,
            Location = 3,
            ProxyAuthentication = 4,
            RetryAfter = 5,
            Server = 6,
            Vary = 7,
            WWWAuthenticate = 8
        }
        public enum EntityHeaders
        {
            Allow = 0,
            ContentEncoding = 1,
            ContentLanguage = 2,
            ContentLength = 3,
            ContentLocation = 4,
            ContentMD5 = 5,
            ContentRange = 6,
            ContentType = 7,
            Expired = 8,
            LastModified = 9
        }

        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private List<Cookie> cookies = new List<Cookie>();

        #region Properties
        public HttpMethods Method { get; set; }
        public string RequestURI { get; set; }
        public string RequestQuery { get; set; }
        public byte[] Body { get; set; }
        public StatusCodes StatusCode { get; set; }
        #endregion
        #region Constructors
        public HttpMessage()
        {
            RequestQuery = string.Empty;
            RequestURI = "/";
            Method = HttpMethods.Get;
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Headers dictionary.
        /// </summary>
        /// <param name="headerKey">Key of the header.</param>
        /// <returns>Value of the header.</returns>
        public string this[string headerKey] { get => headers[headerKey]; set => headers[headerKey] = value; }
        /// <summary>
        /// Headers dictionary.
        /// </summary>
        /// <param name="headerKey">Key of the header.</param>
        /// <returns>Value of the header.</returns>
        public string this[GeneralHeaders headerKey] { get => headers[HttpHeaders.GeneralHeaders[(int)headerKey]]; set => headers[HttpHeaders.GeneralHeaders[(int)headerKey]] = value; }
        /// <summary>
        /// Headers dictionary.
        /// </summary>
        /// <param name="headerKey">Key of the header.</param>
        /// <returns>Value of the header.</returns>
        public string this[ResponseHeaders headerKey] { get => headers[HttpHeaders.ResponseHeaders[(int)headerKey]]; set => headers[HttpHeaders.ResponseHeaders[(int)headerKey]] = value; }
        /// <summary>
        /// Headers dictionary.
        /// </summary>
        /// <param name="headerKey">Key of the header.</param>
        /// <returns>Value of the header.</returns>
        public string this[RequestHeaders headerKey] { get => headers[HttpHeaders.RequestHeaders[(int)headerKey]]; set => headers[HttpHeaders.RequestHeaders[(int)headerKey]] = value; }
        /// <summary>
        /// Headers dictionary.
        /// </summary>
        /// <param name="headerKey">Key of the header.</param>
        /// <returns>Value of the header.</returns>
        public string this[EntityHeaders headerKey] { get => headers[HttpHeaders.EntityHeaders[(int)headerKey]]; set => headers[HttpHeaders.EntityHeaders[(int)headerKey]] = value; }
        /// <summary>
        /// List of cookies.
        /// </summary>
        public List<Cookie> Cookies { get => cookies; }
        /// <summary>
        /// Add a general header to the message.
        /// </summary>
        /// <param name="header">Header key.</param>
        /// <param name="value">Header value.</param>
        public void AddGeneralHeader(GeneralHeaders header, string value)
        {
            headers[HttpHeaders.GeneralHeaders[(int)header]] = value;
        }
        /// <summary>
        /// Add a request header to the message.
        /// </summary>
        /// <param name="requestHeader">Header key.</param>
        /// <param name="value">Header value.</param>
        public void AddRequestHeader(RequestHeaders requestHeader, string value)
        {
            headers[HttpHeaders.RequestHeaders[(int)requestHeader]] = value;
        }
        /// <summary>
        /// Add a response header to the message.
        /// </summary>
        /// <param name="responseHeader">Header key.</param>
        /// <param name="value">Header value.</param>
        public void AddResponseHeader(ResponseHeaders responseHeader, string value)
        {
            headers[HttpHeaders.ResponseHeaders[(int)responseHeader]] = value;
        }
        /// <summary>
        /// Add an entity header to the message.
        /// </summary>
        /// <param name="entityHeader">Header key.</param>
        /// <param name="value">Header value.</param>
        public void AddEntityHeaders(EntityHeaders entityHeader, string value)
        {
            headers[HttpHeaders.EntityHeaders[(int)entityHeader]] = value;
        }
        /// <summary>
        /// Build the request bytes based on the message contents.
        /// </summary>
        /// <returns>Array of bytes.</returns>
        public byte[] BuildRequest()
        {
            StringBuilder requestString = new StringBuilder();
            requestString.Append(HttpHeaders.Methods[(int)Method]).Append(HttpHeaders.SP).Append(RequestURI).Append('?').Append(RequestQuery).Append(HttpHeaders.SP).Append(HttpHeaders.HTTPVER).Append(HttpHeaders.CRLF);
            foreach(KeyValuePair<string, string> header in headers)
            {
                requestString.Append(header.Key).Append(':').Append(HttpHeaders.SP).Append(header.Value).Append(HttpHeaders.CRLF);
            }
            requestString.Append(HttpHeaders.CRLF);
            if(Cookies.Count > 0)
            {
                requestString.Append(HttpHeaders.RequestCookieHeader).Append(':').Append(HttpHeaders.SP);
                for(int i = 0; i < Cookies.Count; i++)
                {
                    Cookie cookie = Cookies[i];
                    requestString.Append(cookie.BuildCookieString());
                    if(i < Cookies.Count - 1)
                    {
                        requestString.Append(';');
                    }
                }
            }
            byte[] request = new byte[requestString.Length + (Body == null ? 0 : Body.Length)];
            byte[] requestBytes = ASCIIEncoding.ASCII.GetBytes(requestString.ToString());
            Array.Copy(requestBytes, 0, request, 0, requestBytes.Length);
            if (Body != null)
            {
                Array.Copy(Body, 0, request, requestBytes.Length, Body.Length);
            }
            return request;
        }
        /// <summary>
        /// Parse the received bytes and populate the message contents.
        /// </summary>
        /// <param name="requestBytes">Message bytes to be parsed.</param>
        public void ParseRequest(byte[] requestBytes)
        {
            /*
             * Parse the bytes one by one, respecting the reference manual.
             */
            StringBuilder parseBuffer = new StringBuilder();
            /*
             * Keep the index of the byte array, to identify the message body.
             * Step value indicates at what point the parsing algorithm currently is.
             * Step 0 - Method, 1 - URI, 2 - Query, 3 - HTTPVer, 4 - Header, 5 - Value
             */
            int step = 0;
            string headerKey = string.Empty;
            string headerValue = string.Empty;
            int bodyIndex = 0;
            for(int i = 0; i < requestBytes.Length; i++)
            {
                if(step == 0)
                {
                    Method = ParseMethod(requestBytes, ref i);
                    step++;
                }
                else if(step == 1)
                {
                    RequestURI = ParseRequestURI(requestBytes, ref i);
                    if(requestBytes[i] == '?')
                    {
                        step++;   
                    }
                    else
                    {
                        step += 2;
                    }                    
                }
                else if(step == 2)
                {
                    RequestQuery = ParseRequestQuery(requestBytes, ref i);
                    step++;
                }
                else if (step == 3)
                {
                    ParseHTTPVer(requestBytes, ref i);
                    step++;
                }
                else if (step == 4)
                {
                    if(requestBytes[i] == HttpHeaders.CRLF[0])
                    {
                        continue;
                    }
                    else if(requestBytes[i] == HttpHeaders.CRLF[1])
                    {
                        bodyIndex = i;
                        break;
                    }
                    else
                    {
                        headerKey = ParseHeaderKey(requestBytes, ref i);
                        step++;
                    }
                }
                else if (step == 5)
                {
                    if (requestBytes[i] == HttpHeaders.CRLF[0])
                    {
                        continue;
                    }
                    else if (requestBytes[i] == HttpHeaders.CRLF[1])
                    {
                        bodyIndex = i;
                        break;
                    }
                    else
                    {
                        headerValue = ParseHeaderValue(requestBytes, ref i);
                        if (headerKey == HttpHeaders.RequestCookieHeader)
                        {
                            Cookies.Add(new Cookie(headerValue));
                        }
                        else
                        {
                            headers.Add(headerKey, headerValue);
                        }
                        step--;
                    }
                }
            }
            if(requestBytes.Length - bodyIndex > 1)
            {
                /*
                 * If the message contains a body, copy it into a different array
                 * and save it into the HTTP message;
                 */
                this.Body = new byte[requestBytes.Length - bodyIndex];
                Array.Copy(requestBytes, bodyIndex, this.Body, 0, this.Body.Length);
            }
            return;
        }
        /// <summary>
        /// Build the response bytes based on the message contents.
        /// </summary>
        /// <param name="includeContentLengthHeader">
        /// If set to true, add an extra Content-Length header specifying the length of the body.
        /// </param>
        /// <returns>Array of bytes.</returns>
        public byte[] BuildResponse(bool includeContentLengthHeader)
        {
            if (includeContentLengthHeader)
            {
                /*
                 * If there is a body, include the size of the body. If there is no body,
                 * set the value of the content length to 0.
                 */
                this[EntityHeaders.ContentLength] = Body == null ? "0" : Body.Length.ToString();
            }
            StringBuilder responseString = new StringBuilder();
            responseString.Append(HttpHeaders.HTTPVER).Append(HttpHeaders.SP).Append((int)this.StatusCode).Append(HttpHeaders.SP).Append(this.StatusCode.ToString()).Append(HttpHeaders.CRLF);
            foreach (KeyValuePair<string, string> header in headers)
            {
                responseString.Append(header.Key).Append(':').Append(HttpHeaders.SP).Append(header.Value).Append(HttpHeaders.CRLF);
            }
            responseString.Append(HttpHeaders.CRLF);
            foreach(Cookie cookie in Cookies)
            {
                responseString.Append(HttpHeaders.ResponseCookieHeader).Append(':').Append(HttpHeaders.SP).Append(cookie.BuildCookieString()).Append(HttpHeaders.CRLF);
            }
            byte[] response = new byte[responseString.Length + (Body == null ? 0 : Body.Length)];
            byte[] responseBytes = ASCIIEncoding.ASCII.GetBytes(responseString.ToString());
            Array.Copy(responseBytes, 0, response, 0, responseBytes.Length);
            if (Body != null)
            {
                Array.Copy(Body, 0, response, responseBytes.Length, Body.Length);
            }
            return response;
        }
        /// <summary>
        /// Parse the received bytes and populate the message contents.
        /// </summary>
        /// <param name="responseBytes">Array of bytes to be parsed.</param>
        public void ParseResponse(byte[] responseBytes)
        {
            /*
             * Parse the bytes one by one, respecting the reference manual.
             */
            StringBuilder parseBuffer = new StringBuilder();
            /*
             * Keep the index of the byte array, to identify the message body.
             * Step value indicates at what point the parsing algorithm currently is.
             * Step 0 - HTTPVer, 1 - StatusCodeInt, 2 - StatusCodeString, 3 - Header, 4 - Value
             */
            int step = 0;
            string headerKey = string.Empty;
            string headerValue = string.Empty;
            int bodyIndex = 0;
            for (int i = 0; i < responseBytes.Length; i++)
            {
                if (step == 0)
                {
                    ParseHTTPVer(responseBytes, ref i);
                    step++;
                }
                else if (step == 1)
                {
                    StatusCode = (StatusCodes)ParseResponseCode(responseBytes, ref i);
                    step++;
                }
                else if (step == 2)
                {
                    if(StatusCode != ParseResponseCodeString(responseBytes, ref i))
                    {
                        throw new InvalidStatusCodeException("Status code value and text do not match!");
                    }
                    step++;
                }
                else if (step == 3)
                {
                    if (responseBytes[i] == HttpHeaders.CRLF[0])
                    {
                        continue;
                    }
                    else if (responseBytes[i] == HttpHeaders.CRLF[1])
                    {
                        bodyIndex = i;
                        break;
                    }
                    else
                    {
                        headerKey = ParseHeaderKey(responseBytes, ref i);
                        step++;
                    }
                }
                else if (step == 4)
                {
                    if (responseBytes[i] == HttpHeaders.CRLF[0])
                    {
                        continue;
                    }
                    else if (responseBytes[i] == HttpHeaders.CRLF[1])
                    {
                        bodyIndex = i;
                        break;
                    }
                    else
                    {
                        headerValue = ParseHeaderValue(responseBytes, ref i);
                        if (headerKey == HttpHeaders.ResponseCookieHeader)
                        {
                            Cookies.Add(new Cookie(headerValue));
                        }
                        else
                        {
                            headers.Add(headerKey, headerValue);
                        }
                        step--;
                    }
                }
            }
            if (responseBytes.Length - bodyIndex > 1)
            {
                /*
                 * If the message contains a body, copy it into a different array
                 * and save it into the HTTP message;
                 */
                this.Body = new byte[responseBytes.Length - bodyIndex - 1];
                Array.Copy(responseBytes, bodyIndex, this.Body, 0, this.Body.Length);
            }
            return;
        }
        /// <summary>
        /// Check if the message contains a header.
        /// </summary>
        /// <param name="header">Key of the header.</param>
        /// <returns>True if the message contains a header with the provided key.</returns>
        public bool ContainsHeader(ResponseHeaders header)
        {
            return ContainsHeader(HttpHeaders.ResponseHeaders[(int)header]);
        }
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
        /// Parse the body into a posted from respecting the reference manual.
        /// </summary>
        /// <returns>Dictionary with posted from.</returns>
        public Dictionary<string, string> GetPostForm()
        {
            if (ContainsHeader("Content-Type") && Body != null)
            {
                if(this["Content-Type"] == "application/x-www-form-urlencoded")
                {
                    Dictionary<string, string> returnDictionary = new Dictionary<string, string>();
                    /*
                     * Walk through the buffer and get the form contents.
                     * Step 0 - key, 1 - value.
                     */
                    string formKey = string.Empty;
                    int step = 0;
                    for(int i = 0; i < Body.Length; i++)
                    {
                        if(step == 0)
                        {
                            formKey = GetField(Body, ref i);
                            step++;
                        }
                        else
                        {
                            returnDictionary[formKey] = GetValue(Body, ref i);
                            step--;
                        }
                    }
                    return returnDictionary;
                }
                else if (this["Content-Type"].Contains("multipart/form-data"))
                {
                    throw new NotImplementedException("Multipart posting not implemented");
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        #endregion
        #region Private Methods
        private HttpMethods GetMethod(string methodString)
        {
            int index = Array.IndexOf(HttpHeaders.Methods, methodString.ToUpper());
            return (HttpMethods)index;
        }

        private HttpMethods ParseMethod(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the method, clear the buffer
             * and continue with parsing the next step.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == (byte)HttpHeaders.SP)
                    {
                        string methodString = parseBuffer.ToString();
                        return GetMethod(methodString);
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidMethodException("Invalid request method. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidMethodException("Invalid request method. Buffer: " + parseBuffer.ToString());
        }

        private string ParseRequestURI(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the URI and clear the buffer.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == (byte)HttpHeaders.SP)
                    {
                        return parseBuffer.ToString();
                    }
                    if (buffer[index] == '?')
                    {
                        return parseBuffer.ToString();
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidRequestURIException("Invalid request URI. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidRequestURIException("Invalid request URI. Buffer: " + parseBuffer.ToString());
        }

        private string ParseRequestQuery(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the URI and clear the buffer.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == (byte)HttpHeaders.SP)
                    {
                        return parseBuffer.ToString();
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidRequestURIException("Invalid request query. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidRequestURIException("Invalid request query. Buffer: " + parseBuffer.ToString());
        }

        private void ParseHTTPVer(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the HTTPVer.
             * Check if the HTTPVer matches the implementation version.
             * If not, throw an exception.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == HttpHeaders.CRLF[1] || buffer[index] == HttpHeaders.SP)
                    {
                        string httpVer = parseBuffer.ToString();
                        if (httpVer != HttpHeaders.HTTPVER)
                        {
                            throw new InvalidHttpVersionException("Invalid HTTP version. Buffer: " + parseBuffer.ToString());
                        }
                        return;
                    }
                    else if(buffer[index] == HttpHeaders.CRLF[0])
                    {
                        /*
                         * If a termination character is detected, ignore it and wait for the full terminator.
                         */
                        continue;
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidHttpVersionException("Invalid HTTP version. Buffer: " + parseBuffer.ToString(), e);
                }
            }
        }

        private string ParseHeaderKey(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a ':' character, parse the header key.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == ':')
                    {
                        return parseBuffer.ToString();
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidHeaderException("Invalid Header key. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidHeaderException("Invalid Header key. Buffer: " + parseBuffer.ToString());
        }

        private string ParseHeaderValue(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == HttpHeaders.CRLF[1])
                    {
                        return parseBuffer.ToString().Trim();
                    }
                    else if (buffer[index] == HttpHeaders.CRLF[0])
                    {
                        /*
                         * If a termination character is detected, ignore it and wait for the full terminator.
                         */
                        continue;
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidHeaderException("Invalid header value. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidHeaderException("Invalid header value. Buffer: " + parseBuffer.ToString());
        }

        private int ParseResponseCode(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the value.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == HttpHeaders.SP)
                    {
                        return int.Parse(parseBuffer.ToString());
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidStatusCodeException("Invalid status code. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidStatusCodeException("Invalid status code. Buffer: " + parseBuffer.ToString());
        }

        private StatusCodes ParseResponseCodeString(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == HttpHeaders.CRLF[1])
                    {
                        return (StatusCodes)Enum.Parse(typeof(StatusCodes), parseBuffer.ToString().Trim());
                    }
                    else if (buffer[index] == HttpHeaders.CRLF[0])
                    {
                        /*
                         * If a termination character is detected, ignore it and wait for the full terminator.
                         */
                        continue;
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidStatusCodeException("Invalid status code. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidHeaderException("Invalid status code. Buffer: " + parseBuffer.ToString());
        }

        private string GetField(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == '=')
                    {
                        return parseBuffer.ToString().Trim();
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidPostFormException("Invalid form field. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new InvalidHeaderException("Invalid form field. Buffer: " + parseBuffer.ToString());
        }

        private string GetValue(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            StringBuilder parseBuffer = new StringBuilder();
            for (; index < buffer.Length; index++)
            {
                try
                {
                    if (buffer[index] == '&')
                    {
                        return parseBuffer.ToString().Trim();
                    }
                    else
                    {
                        parseBuffer.Append((char)buffer[index]);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidPostFormException("Invalid form field. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            return parseBuffer.ToString().Trim();
        }
        #endregion
    }
}
