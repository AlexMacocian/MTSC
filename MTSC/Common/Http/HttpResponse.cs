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
        public string BodyString { get => ASCIIEncoding.ASCII.GetString(Body); set => Body = ASCIIEncoding.ASCII.GetBytes(value); }
        public StatusCodes StatusCode { get; set; }
        public HttpResponseHeaderDictionary Headers { get; } = new HttpResponseHeaderDictionary();

        public HttpResponse()
        {

        }

        public HttpResponse(byte[] responseBytes)
        {
            ParseResponse(responseBytes);
        }

        public static HttpResponse FromBytes(byte[] responseBytes)
        {
            return new HttpResponse(responseBytes);
        }

        public byte[] GetPackedResponse(bool includeContentLengthHeader)
        {
            return BuildResponse(includeContentLengthHeader);
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
                Headers[EntityHeaders.ContentLength] = Body == null ? "0" : Body.Length.ToString();
            }
            StringBuilder responseString = new StringBuilder();
            responseString.Append(HttpHeaders.HTTPVER).Append(HttpHeaders.SP).Append((int)this.StatusCode).Append(HttpHeaders.SP).Append(this.StatusCode.ToString()).Append(HttpHeaders.CRLF);
            foreach (KeyValuePair<string, string> header in Headers)
            {
                responseString.Append(header.Key).Append(':').Append(HttpHeaders.SP).Append(header.Value).Append(HttpHeaders.CRLF);
            }
            foreach (Cookie cookie in Cookies)
            {
                responseString.Append(HttpHeaders.ResponseCookieHeader).Append(':').Append(HttpHeaders.SP).Append(cookie.BuildCookieString()).Append(HttpHeaders.CRLF);
            }
            responseString.Append(HttpHeaders.CRLF);
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
        private void ParseResponse(byte[] responseBytes)
        {
            /*
             * Parse the bytes one by one, respecting the reference manual.
             */
            MemoryStream ms = new MemoryStream(responseBytes);
            /*
             * Keep the index of the byte array, to identify the message body.
             * Step value indicates at what point the parsing algorithm currently is.
             * Step 0 - HTTPVer, 1 - StatusCodeInt, 2 - StatusCodeString, 3 - Header, 4 - Value
             */
            int step = 0;
            string headerKey = string.Empty;
            string headerValue = string.Empty;
            while (ms.Position < ms.Length)
            {
                if (step == 0)
                {
                    ParseHTTPVer(ms);
                    step++;
                }
                else if (step == 1)
                {
                    StatusCode = (StatusCodes)ParseResponseCode(ms);
                    step++;
                }
                else if (step == 2)
                {
                    if (StatusCode != ParseResponseCodeString(ms))
                    {
                        throw new InvalidStatusCodeException("Status code value and text do not match!");
                    }
                    step++;
                }
                else if (step == 3)
                {
                    char c = (char)ms.ReadByte();
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
                        headerKey = ParseHeaderKey(ms);
                        step++;
                    }
                }
                else if (step == 4)
                {
                    char c = (char)ms.ReadByte();
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
                        headerValue = ParseHeaderValue(ms);
                        if (headerKey == HttpHeaders.ResponseCookieHeader)
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
            StringBuilder parseBuffer = new StringBuilder();
            while(ms.Position < ms.Length)
            {
                try
                {
                    char c = (char)ms.ReadByte();
                    if (c == HttpHeaders.CRLF[1] || c == HttpHeaders.SP)
                    {
                        string httpVer = parseBuffer.ToString();
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
            StringBuilder parseBuffer = new StringBuilder();
            while(ms.Position < ms.Length)
            {
                char c = (char)ms.ReadByte();
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
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                char c = (char)ms.ReadByte();
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
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                char c = (char)ms.ReadByte();
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

        private StatusCodes ParseResponseCodeString(MemoryStream ms)
        {
            /*
             * Get each character one by one. When meeting a LF character, parse the value.
             */
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                char c = (char)ms.ReadByte();
                try
                {
                    if (c == HttpHeaders.CRLF[1])
                    {
                        return (StatusCodes)Enum.Parse(typeof(StatusCodes), parseBuffer.ToString().Trim());
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
