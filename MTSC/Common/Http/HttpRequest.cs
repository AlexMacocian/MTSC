﻿using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http
{
    /// <summary>
    /// Class encapsulating a http request.
    /// </summary>
    public class HttpRequest
    {
        public HttpRequestHeaderDictionary Headers { get; } = new HttpRequestHeaderDictionary();

        /// <summary>
        /// List of cookies.
        /// </summary>
        public List<Cookie> Cookies { get; } = new List<Cookie>();

        public Dictionary<string, string> Form { get; } = new Dictionary<string, string>();
        public MethodEnum Method { get; set; }
        public string RequestURI { get; set; }
        public string RequestQuery { get; set; }
        public byte[] Body { get; set; } = new byte[0];
        public string BodyString { get => ASCIIEncoding.ASCII.GetString(Body); set => Body = ASCIIEncoding.ASCII.GetBytes(value); }

        public HttpRequest()
        {

        }

        public HttpRequest(byte[] requestBytes)
        {
            ParseRequest(requestBytes);
            if(this.Method == MethodEnum.Post)
            {
                Form = GetPostForm();
            }
        }

        public static HttpRequest FromBytes(byte[] requestBytes)
        {
            return new HttpRequest(requestBytes);
        }

        public byte[] GetPackedRequest()
        {
            return BuildRequest();
        }

        private MethodEnum GetMethod(string methodString)
        {
            int index = Array.IndexOf(HttpHeaders.methods, methodString.ToUpper());
            return (MethodEnum)index;
        }

        private MethodEnum ParseMethod(byte[] buffer, ref int index)
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
                    throw new MethodInvalidException("Invalid request method. Buffer: " + parseBuffer.ToString(), e);
                }
            }
            throw new MethodInvalidException("Invalid request method. Buffer: " + parseBuffer.ToString());
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

        /// <summary>
        /// Build the request bytes based on the message contents.
        /// </summary>
        /// <returns>Array of bytes.</returns>
        private byte[] BuildRequest()
        {
            StringBuilder requestString = new StringBuilder();
            requestString.Append(HttpHeaders.methods[(int)Method]).Append(HttpHeaders.SP).Append(RequestURI).Append('?').Append(RequestQuery).Append(HttpHeaders.SP).Append(HttpHeaders.HTTPVER).Append(HttpHeaders.CRLF);
            foreach (KeyValuePair<string, string> header in Headers)
            {
                requestString.Append(header.Key).Append(':').Append(HttpHeaders.SP).Append(header.Value).Append(HttpHeaders.CRLF);
            }
            requestString.Append(HttpHeaders.CRLF);
            if (Cookies.Count > 0)
            {
                requestString.Append(HttpHeaders.RequestCookieHeader).Append(':').Append(HttpHeaders.SP);
                for (int i = 0; i < Cookies.Count; i++)
                {
                    Cookie cookie = Cookies[i];
                    requestString.Append(cookie.BuildCookieString());
                    if (i < Cookies.Count - 1)
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
        private void ParseRequest(byte[] requestBytes)
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
            for (int i = 0; i < requestBytes.Length; i++)
            {
                if (step == 0)
                {
                    Method = ParseMethod(requestBytes, ref i);
                    step++;
                }
                else if (step == 1)
                {
                    RequestURI = ParseRequestURI(requestBytes, ref i);
                    if (requestBytes[i] == '?')
                    {
                        step++;
                    }
                    else
                    {
                        step += 2;
                    }
                }
                else if (step == 2)
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
                            Headers[headerKey] = headerValue;
                        }
                        step--;
                    }
                }
            }
            if (requestBytes.Length - bodyIndex > 1)
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
        /// Parse the body into a posted from respecting the reference manual.
        /// </summary>
        /// <returns>Dictionary with posted from.</returns>
        private Dictionary<string, string> GetPostForm()
        {
            if (Headers.ContainsHeader("Content-Type") && Body != null)
            {
                if (Headers["Content-Type"] == "application/x-www-form-urlencoded")
                {
                    Dictionary<string, string> returnDictionary = new Dictionary<string, string>();
                    /*
                     * Walk through the buffer and get the form contents.
                     * Step 0 - key, 1 - value.
                     */
                    string formKey = string.Empty;
                    int step = 0;
                    for (int i = 0; i < Body.Length; i++)
                    {
                        if (step == 0)
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
                else if (Headers["Content-Type"].Contains("multipart/form-data"))
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
    }
}