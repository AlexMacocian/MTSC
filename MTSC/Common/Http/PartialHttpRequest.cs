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
        public byte[] Body { get; set; } = new byte[0];
        public string BodyString { get => ASCIIEncoding.ASCII.GetString(this.Body).Trim('\0'); set => this.Body = ASCIIEncoding.ASCII.GetBytes(value); }

        public PartialHttpRequest()
        {
        }

        public PartialHttpRequest(byte[] requestBytes)
        {
            this.ParseRequest(requestBytes);
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

        public void AddToBody(byte[] bytesToBeAdded)
        {
            var newBody = new byte[this.Body.Length + bytesToBeAdded.Length];
            if (this.Body.Length > 0)
            {
                Array.Copy(this.Body, 0, newBody, 0, this.Body.Length);
            }

            if (bytesToBeAdded.Length > 0)
            {
                Array.Copy(bytesToBeAdded, 0, newBody, this.Body.Length, bytesToBeAdded.Length);
            }

            this.Body = newBody;
            if (this.Headers.ContainsHeader(EntityHeaders.ContentLength) && int.Parse(this.Headers[EntityHeaders.ContentLength]) == this.Body.Length)
            {
                this.Complete = true;
            }
        }

        private HttpMethods GetMethod(string methodString)
        {
            return (HttpMethods)Enum.Parse(typeof(HttpMethods), methodString.ToUpper(), true);
        }

        private HttpMethods ParseMethod(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the method, clear the buffer
             * and continue with parsing the next step.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == HttpHeaders.SP)
                    {
                        var methodString = parseBuffer.ToString();
                        return this.GetMethod(methodString);
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

            throw new IncompleteMethodException("Incomplete request method. Buffer: " + parseBuffer.ToString(),
                new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
        }

        private string ParseRequestURI(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the URI and clear the buffer.
             */
            var parseBuffer = new StringBuilder();
            ms.ReadByte(); //Ignore the first '/'
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == HttpHeaders.SP)
                    {
                        return parseBuffer.ToString();
                    }

                    if (c == '?')
                    {
                        return parseBuffer.ToString();
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

            throw new IncompleteRequestURIException("Incomplete request URI. Buffer: " + parseBuffer.ToString(),
                new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
        }

        private string ParseRequestQuery(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the URI and clear the buffer.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == (byte)HttpHeaders.SP)
                    {
                        return parseBuffer.ToString();
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

            throw new IncompleteRequestQueryException("Incomplete request query. Buffer: " + parseBuffer.ToString(),
                new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
        }

        private void ParseHTTPVer(MemoryStream ms)
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

                        return;
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
            throw new IncompleteHttpVersionException("Incomplete HTTP version. Buffer: " + parseBuffer.ToString(),
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
        }

        private string ParseHeaderKey(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a ':' character, parse the header key.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == ':')
                    {
                        return parseBuffer.ToString();
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

            throw new IncompleteHeaderKeyException("Incomplete Header key. Buffer: " + parseBuffer.ToString(),
                new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
        }

        private string ParseHeaderValue(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    var c = (char)ms.ReadByte();
                    if (c == HttpHeaders.CRLF[1])
                    {
                        return parseBuffer.ToString().Trim();
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

            throw new IncompleteHeaderValueException("Incomplete header value. Buffer: " + parseBuffer.ToString(),
                new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
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
            var ms = new MemoryStream(requestBytes);
            /*
             * Keep the index of the byte array, to identify the message body.
             * Step value indicates at what point the parsing algorithm currently is.
             * Step 0 - Method, 1 - URI, 2 - Query, 3 - HTTPVer, 4 - Header, 5 - Value
             */
            var step = 0;
            var headerKey = string.Empty;
            var headerValue = string.Empty;
            while (ms.Position < ms.Length)
            {
                if (step == 0)
                {
                    this.Method = this.ParseMethod(ms);
                    step++;
                }
                else if (step == 1)
                {
                    this.RequestURI = this.ParseRequestURI(ms);
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
                    this.RequestQuery = this.ParseRequestQuery(ms);
                    step++;
                }
                else if (step == 3)
                {
                    this.ParseHTTPVer(ms);
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
                        headerKey = this.ParseHeaderKey(ms);
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
                        headerValue = this.ParseHeaderValue(ms);
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
                throw new IncompleteRequestException($"Incomplete request.",
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
            }

            this.HeaderByteCount = (int)ms.Position;
            if (this.Headers.ContainsHeader(EntityHeaders.ContentLength))
            {
                var remainingBytes = int.Parse(this.Headers[EntityHeaders.ContentLength]);
                if (remainingBytes <= ms.Length - ms.Position)
                {
                    this.Body = ms.ReadRemainingBytes();
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (ms.Length - ms.Position > 1)
                {
                    /*
                     * If the message contains a body, copy it into a different array
                     * and save it into the HTTP message;
                     */
                    this.Body = ms.ReadRemainingBytes();
                }
            }

            this.Complete = true;
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
