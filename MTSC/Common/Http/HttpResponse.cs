using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http
{
    public class HttpResponse
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
            /*
             * Since set-cookie is being put into the body, first calculate the length of set-cookie lines
             * in order to obtain the correct contentlength header value.
             */ 
            var cookieString = new StringBuilder();
            foreach (Cookie cookie in Cookies)
            {
                cookieString.Append(HttpHeaders.ResponseCookieHeader).Append(':').Append(HttpHeaders.SP).Append(cookie.BuildCookieString()).Append(HttpHeaders.CRLF);
            }

            if (includeContentLengthHeader)
            {
                /*
                 * If there is a body, include the size of the body. If there is no body,
                 * set the value of the content length to 0.
                 */
                Headers[EntityHeadersEnum.ContentLength] = Body == null ? "0" : (Body.Length + cookieString.Length).ToString();
            }
            StringBuilder responseString = new StringBuilder();
            responseString.Append(HttpHeaders.HTTPVER).Append(HttpHeaders.SP).Append((int)this.StatusCode).Append(HttpHeaders.SP).Append(this.StatusCode.ToString()).Append(HttpHeaders.CRLF);
            foreach (KeyValuePair<string, string> header in Headers)
            {
                responseString.Append(header.Key).Append(':').Append(HttpHeaders.SP).Append(header.Value).Append(HttpHeaders.CRLF);
            }
            responseString.Append(cookieString);
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
                    if (StatusCode != ParseResponseCodeString(responseBytes, ref i))
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
                            Headers[headerKey] = headerValue;
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
    }
}
