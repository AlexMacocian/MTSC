using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http
{
    public sealed class HttpResponse
    {
        /// <summary>
        /// List of cookies.
        /// </summary>
        public List<Cookie> Cookies { get; } = new List<Cookie>();
        public byte[] Body { get; set; } = new byte[0];
        public string BodyString { get => Encoding.ASCII.GetString(this.Body); set => this.Body = Encoding.ASCII.GetBytes(value); }
        public StatusCodes StatusCode { get; set; }
        public string StatusString { get; set; } = string.Empty;
        public HttpResponseHeaderDictionary Headers { get; } = new HttpResponseHeaderDictionary();

        public HttpResponse()
        {

        }

        public HttpResponse(byte[] responseBytes)
        {
            this.ParseResponse(responseBytes);
        }

        public static HttpResponse FromBytes(byte[] responseBytes)
        {
            return new HttpResponse(responseBytes);
        }

        public byte[] GetPackedResponse(bool includeContentLengthHeader)
        {
            return this.BuildResponse(includeContentLengthHeader);
        }

        /// <summary>
        /// Build the response bytes based on the message contents.
        /// </summary>
        /// <param name="includeContentLengthHeader">
        /// If set to true, add an extra Content-Length header specifying the length of the body.
        /// </param>
        /// <returns>Array of bytes.</returns>
        private byte[] BuildResponse(bool includeContentLengthHeader)
        {
            if (includeContentLengthHeader)
            {
                /*
                 * If there is a body, include the size of the body. If there is no body,
                 * set the value of the content length to 0.
                 */
                this.Headers[EntityHeaders.ContentLength] = this.Body == null ? "0" : this.Body.Length.ToString();
            }

            var responseString = new StringBuilder();
            responseString.Append(HttpHeaders.HTTPVER).Append(HttpHeaders.SP)
                .Append((int)this.StatusCode).Append(HttpHeaders.SP)
                .Append(this.StatusString != string.Empty ? this.StatusString : this.StatusCode.ToString()).Append(HttpHeaders.CRLF);
            foreach (var header in this.Headers)
            {
                responseString.Append(header.Key).Append(':').Append(HttpHeaders.SP).Append(header.Value).Append(HttpHeaders.CRLF);
            }

            foreach (var cookie in this.Cookies)
            {
                responseString.Append(HttpHeaders.ResponseCookieHeader).Append(':').Append(HttpHeaders.SP).Append(cookie.BuildCookieString()).Append(HttpHeaders.CRLF);
            }

            responseString.Append(HttpHeaders.CRLF);
            var response = new byte[responseString.Length + (this.Body == null ? 0 : this.Body.Length)];
            var responseBytes = Encoding.ASCII.GetBytes(responseString.ToString());
            Array.Copy(responseBytes, 0, response, 0, responseBytes.Length);
            if (this.Body != null)
            {
                Array.Copy(this.Body, 0, response, responseBytes.Length, this.Body.Length);
            }

            return response;
        }
        /// <summary>
        /// Parse the received bytes and populate the message contents.
        /// </summary>
        /// <param name="responseBytes">Array of bytes to be parsed.</param>
        private void ParseResponse(byte[] responseBytes)
        {
            /*
             * Parse the bytes one by one, respecting the reference manual.
             */
            var ms = new MemoryStream(responseBytes);
            /*
             * Keep the index of the byte array, to identify the message body.
             * Step value indicates at what point the parsing algorithm currently is.
             * Step 0 - HTTPVer, 1 - StatusCodeInt, 2 - StatusCodeString, 3 - Header, 4 - Value
             */
            var step = 0;
            var headerKey = string.Empty;
            var headerValue = string.Empty;
            while (ms.Position < ms.Length)
            {
                if (step == 0)
                {
                    this.ParseHTTPVer(ms);
                    step++;
                }
                else if (step == 1)
                {
                    this.StatusCode = (StatusCodes)this.ParseResponseCode(ms);
                    step++;
                }
                else if (step == 2)
                {
                    this.StatusString = this.ParseResponseCodeString(ms);
                    step++;
                }
                else if (step == 3)
                {
                    var c = (char)ms.ReadByte();
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
                else if (step == 4)
                {
                    var c = (char)ms.ReadByte();
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
                        if (headerKey == HttpHeaders.ResponseCookieHeader)
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

            if (ms.Length - ms.Position > 1)
            {
                /*
                 * If the message contains a body, copy it into a different array
                 * and save it into the HTTP message;
                 */
                this.Body = ms.ReadRemainingBytes();
            }

            return;
        }
        private void ParseHTTPVer(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the HTTPVer.
             * Check if the HTTPVer matches the implementation version.
             * If not, throw an exception.
             */
            var parseBuffer = new StringBuilder();
            while(ms.Position < ms.Length)
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
                    throw new InvalidHttpVersionException("Invalid HTTP version. Buffer: " + parseBuffer.ToString(), e);
                }
            }
        }


        private string ParseHeaderKey(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a ':' character, parse the header key.
             */
            var parseBuffer = new StringBuilder();
            while(ms.Position < ms.Length)
            {
                var c = (char)ms.ReadByte();
                try
                {
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
                    throw new InvalidHeaderException("Invalid Header key. Buffer: " + parseBuffer.ToString(), e);
                }
            }

            throw new InvalidHeaderException("Invalid Header key. Buffer: " + parseBuffer.ToString());
        }

        private string ParseHeaderValue(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                var c = (char)ms.ReadByte();
                try
                {
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
                    throw new InvalidHeaderException("Invalid header value. Buffer: " + parseBuffer.ToString(), e);
                }
            }

            throw new InvalidHeaderException("Invalid header value. Buffer: " + parseBuffer.ToString());
        }

        private int ParseResponseCode(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a SP character, parse the value.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                var c = (char)ms.ReadByte();
                try
                {
                    if (c == HttpHeaders.SP)
                    {
                        return int.Parse(parseBuffer.ToString());
                    }
                    else
                    {
                        parseBuffer.Append(c);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidStatusCodeException("Invalid status code. Buffer: " + parseBuffer.ToString(), e);
                }
            }

            throw new InvalidStatusCodeException("Invalid status code. Buffer: " + parseBuffer.ToString());
        }

        private string ParseResponseCodeString(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            var parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                var c = (char)ms.ReadByte();
                try
                {
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
                    throw new InvalidStatusCodeException("Invalid status code. Buffer: " + parseBuffer.ToString(), e);
                }
            }

            throw new InvalidHeaderException("Invalid status code. Buffer: " + parseBuffer.ToString());
        }
    }
}
