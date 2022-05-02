using MTSC.Common.Http.Forms;
using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http
{
    internal class PartialHttpRequest
    {
        private readonly MemoryStream requestBuffer = new();

        public long BufferLength => this.requestBuffer.Length;
        public HttpRequestHeaderDictionary Headers { get; } = new HttpRequestHeaderDictionary();
        /// <summary>
        /// List of cookies.
        /// </summary>
        public List<Cookie> Cookies { get; } = new List<Cookie>();
        public bool Complete { get; private set; } = false;
        public int HeaderByteCount { get; private set; } = 0;
        public Form Form { get; } = new Form();
        public HttpMethods Method { get; set; }
        public string RequestURI { get; set; }
        public string RequestQuery { get; set; }
        public byte[] Body { get; set; } = Array.Empty<byte>();
        public string BodyString { get => ASCIIEncoding.ASCII.GetString(this.Body).Trim('\0'); set => this.Body = ASCIIEncoding.ASCII.GetBytes(value); }

        public PartialHttpRequest()
        {
        }

        public PartialHttpRequest(byte[] requestBytes)
        {
            this.requestBuffer.Write(requestBytes, 0, requestBytes.Length);
            this.ParseRequest();
        }

        public static PartialHttpRequest FromBytes(byte[] requestBytes)
        {
            return new PartialHttpRequest(requestBytes);
        }

        public HttpRequest ToRequest()
        {
            var httpRequest = new HttpRequest();
            foreach(var header in this.Headers)
            {
                httpRequest.Headers[header.Key] = header.Value;
            }

            httpRequest.Method = this.Method;
            httpRequest.RequestQuery = this.RequestQuery;
            httpRequest.RequestURI = this.RequestURI;
            httpRequest.Body = this.Body;
            foreach(var cookie in this.Cookies)
            {
                httpRequest.Cookies.Add(cookie);
            }

            httpRequest.ParseBodyForm();
            return httpRequest;
        }

        public void AppendBytes(byte[] bytesToBeAdded)
        {
            this.requestBuffer.Seek(0, SeekOrigin.End);
            this.requestBuffer.Write(bytesToBeAdded, 0, bytesToBeAdded.Length);
            this.ParseRequest();
        }

        private bool TryParseMethod(MemoryStream ms, out HttpMethods method)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the method, clear the buffer
             * and continue with parsing the next step.
             */
            method = HttpMethods.Get;
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == HttpHeaders.SP)
                    {
                        var methodString = parseBuffer.ToString();
                        if (Enum.TryParse(methodString, ignoreCase: true, out method) is false)
                        {
                            return false;
                        }

                        return true;
                    }
                    else
                    {
                        parseBuffer.Append(c);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidMethodException("Invalid request method. Buffer: " + parseBuffer.ToString(),
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray()), e));
                }
            }

            return false;
        }

        private bool TryParseRequestURI(MemoryStream ms, out string requestUri)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the URI and clear the buffer.
             */
            requestUri = string.Empty;
            var parseBuffer = new StringBuilder();
            ms.ReadByte(); //Ignore the first '/'
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == HttpHeaders.SP)
                    {
                        requestUri = parseBuffer.ToString();
                        return true;
                    }

                    if (c == '?')
                    {
                        requestUri = parseBuffer.ToString();
                        return true;
                    }
                    else
                    {
                        parseBuffer.Append(c);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidRequestURIException("Invalid request URI. Buffer: " + parseBuffer.ToString(),
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray()), e));
                }
            }

            return false;
        }

        private bool ParseRequestQuery(MemoryStream ms, out string requestQuery)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the URI and clear the buffer.
             */
            requestQuery = string.Empty;
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == (byte)HttpHeaders.SP)
                    {
                        requestQuery = parseBuffer.ToString();
                        return true;
                    }
                    else
                    {
                        parseBuffer.Append(c);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidRequestURIException("Invalid request query. Buffer: " + parseBuffer.ToString(),
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray()), e));
                }
            }

            return false;
        }

        private bool TryParseHTTPVer(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the HTTPVer.
             * Check if the HTTPVer matches the implementation version.
             * If not, throw an exception.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == HttpHeaders.CRLF[1] || c == HttpHeaders.SP)
                    {
                        var httpVer = parseBuffer.ToString();
                        if (httpVer != HttpHeaders.HTTPVER)
                        {
                            throw new InvalidHttpVersionException("Invalid HTTP version. Buffer: " + parseBuffer.ToString());
                        }

                        return true;
                    }
                    else if (c == HttpHeaders.CRLF[0])
                    {
                        /*
                         * If a termination character is detected, ignore it and wait for the full terminator.
                         */
                        continue;
                    }
                    else
                    {
                        parseBuffer.Append(c);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidHttpVersionException("Invalid HTTP version. Buffer: " + parseBuffer.ToString(),
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray()), e));
                }
            }
            // If code reaches here, it means the message is incomplete.
            return false;
        }

        private bool TryParseHeaderKey(MemoryStream ms, out string headerKey)
        {
            /*
             * Get each character one by one. When meeting a ':' character, parse the header key.
             */
            headerKey = string.Empty;
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == ':')
                    {
                        headerKey = parseBuffer.ToString();
                        return true;
                    }
                    else
                    {
                        parseBuffer.Append(c);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidHeaderException("Invalid Header key. Buffer: " + parseBuffer.ToString(),
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray()), e));
                }
            }

            return false;
        }

        private bool TryParseHeaderValue(MemoryStream ms, out string headerValue)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            headerValue = string.Empty;
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == HttpHeaders.CRLF[1])
                    {
                        headerValue = parseBuffer.ToString().Trim();
                        return true;
                    }
                    else if (c == HttpHeaders.CRLF[0])
                    {
                        /*
                         * If a termination character is detected, ignore it and wait for the full terminator.
                         */
                        continue;
                    }
                    else
                    {
                        parseBuffer.Append(c);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidHeaderException("Invalid header value. Buffer: " + parseBuffer.ToString(),
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray()), e));
                }
            }

            return false;
        }

        /// <summary>
        /// Parse the received bytes and populate the message contents.
        /// </summary>
        /// <param name="requestBytes">Message bytes to be parsed.</param>
        private void ParseRequest()
        {
            /*
             * Parse the bytes one by one, respecting the reference manual.
             */
            this.requestBuffer.Seek(0, SeekOrigin.Begin);
            var ms = new MemoryStream(this.requestBuffer.ToArray());
            /*
             * Keep the index of the byte array, to identify the message body.
             * Step value indicates at what point the parsing algorithm currently is.
             * Step 0 - Method, 1 - URI, 2 - Query, 3 - HTTPVer, 4 - Header, 5 - Value
             */
            var step = 0;
            var headerKey = string.Empty;
            while (ms.Position < ms.Length)
            {
                if (step == 0)
                {
                    if (this.TryParseMethod(ms, out var method) is false)
                    {
                        return;
                    }

                    this.Method = method;
                    step++;
                }
                else if (step == 1)
                {
                    if (this.TryParseRequestURI(ms, out var requestUri) is false)
                    {
                        return;
                    }

                    this.RequestURI = requestUri;
                    ms.Seek(-1, SeekOrigin.Current);
                    if (ms.ReadByte() == '?')
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
                    if (this.ParseRequestQuery(ms, out var requestQuery) is false)
                    {
                        return;
                    }

                    this.RequestQuery = requestQuery;
                    step++;
                }
                else if (step == 3)
                {
                    if (this.TryParseHTTPVer(ms) is false)
                    {
                        return;
                    }

                    step++;
                }
                else if (step == 4)
                {
                    var c = Convert.ToChar(ms.ReadByte());
                    if (c == HttpHeaders.CRLF[0])
                    {
                        continue;
                    }
                    else if (c == HttpHeaders.CRLF[1])
                    {
                        break;
                    }
                    else
                    {
                        ms.Seek(-1, SeekOrigin.Current);
                        if (this.TryParseHeaderKey(ms, out headerKey) is false)
                        {
                            return;
                        }

                        step++;
                    }
                }
                else if (step == 5)
                {
                    var c = Convert.ToChar(ms.ReadByte());
                    if (c == HttpHeaders.CRLF[0])
                    {
                        continue;
                    }
                    else if (c == HttpHeaders.CRLF[1])
                    {
                        break;
                    }
                    else
                    {
                        ms.Seek(-1, SeekOrigin.Current);
                        if (this.TryParseHeaderValue(ms, out var headerValue) is false)
                        {
                            return;
                        }

                        if (headerKey == HttpHeaders.RequestCookieHeader)
                        {
                            this.Cookies.Add(new Cookie(headerValue));
                        }
                        else
                        {
                            this.Headers[headerKey] = headerValue;
                        }

                        step--;
                    }
                }
            }

            if (step < 4)
            {
                return;
            }

            this.HeaderByteCount = (int)ms.Position;
            this.Body = ms.ReadRemainingBytes();
            if (this.Headers.ContainsHeader(EntityHeaders.ContentLength) is false)
            {

                this.Complete = true;
                return;
            }

            var contentLength = int.Parse(this.Headers[EntityHeaders.ContentLength]);
            if (this.Body.Length >= contentLength)
            {
                this.Complete = true;
            }

            return;
        }
        private string GetField(byte[] buffer, ref int index)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            var parseBuffer = new StringBuilder();
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
            var parseBuffer = new StringBuilder();
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
