using MTSC.Common.Http.Forms;
using MTSC.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http
{
    class PartialHttpRequest
    {
        public HttpRequestHeaderDictionary Headers { get; } = new HttpRequestHeaderDictionary();

        /// <summary>
        /// List of cookies.
        /// </summary>
        public List<Cookie> Cookies { get; } = new List<Cookie>();
        public bool Complete { get; private set; } = false;
        public Form Form { get; } = new Form();
        public HttpMethods Method { get; set; }
        public string RequestURI { get; set; }
        public string RequestQuery { get; set; }
        public byte[] Body { get; set; } = new byte[0];
        public string BodyString { get => ASCIIEncoding.ASCII.GetString(Body).Trim('\0'); set => Body = ASCIIEncoding.ASCII.GetBytes(value); }

        public PartialHttpRequest()
        {

        }

        public PartialHttpRequest(byte[] requestBytes)
        {
            ParseRequest(requestBytes);
            if (this.Method == HttpMethods.Post)
            {
                Form = GetPostForm();
            }
        }

        public static PartialHttpRequest FromBytes(byte[] requestBytes)
        {
            return new PartialHttpRequest(requestBytes);
        }

        public HttpRequest ToRequest()
        {
            HttpRequest httpRequest = new HttpRequest();
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
            foreach(var kvp in this.Form)
            {
                httpRequest.Form.SetValue(kvp.Key, kvp.Value);
            }
            return httpRequest;
        }

        public byte[] GetPackedRequest()
        {
            return BuildRequest();
        }

        public void AddToBody(byte[] bytesToBeAdded)
        {
            var newBody = new byte[Body.Length + bytesToBeAdded.Length];
            if (Body.Length > 0)
            {
                Array.Copy(Body, 0, newBody, 0, Body.Length);
            }
            if (bytesToBeAdded.Length > 0)
            {
                Array.Copy(bytesToBeAdded, 0, newBody, Body.Length, bytesToBeAdded.Length);
            }
            Body = newBody;
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
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    char c = (char)ms.ReadByte();
                    if (c == HttpHeaders.SP)
                    {
                        string methodString = parseBuffer.ToString();
                        return GetMethod(methodString);
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
            StringBuilder parseBuffer = new StringBuilder();
            ms.ReadByte(); //Ignore the first '/'
            while (ms.Position < ms.Length)
            {
                try
                {
                    char c = (char)ms.ReadByte();
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
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    char c = (char)ms.ReadByte();
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
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
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
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    char c = (char)ms.ReadByte();
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
            StringBuilder parseBuffer = new StringBuilder();
            while (ms.Position < ms.Length)
            {
                try
                {
                    char c = (char)ms.ReadByte();
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
        /// Build the request bytes based on the message contents.
        /// </summary>
        /// <returns>Array of bytes.</returns>
        private byte[] BuildRequest()
        {
            StringBuilder requestString = new StringBuilder();
            requestString.Append(HttpHeaders.Methods[(int)Method]).Append(HttpHeaders.SP).Append(RequestURI);
            if (!string.IsNullOrWhiteSpace(RequestQuery))
            {
                requestString.Append('?').Append(RequestQuery);
            }
            requestString.Append(HttpHeaders.SP).Append(HttpHeaders.HTTPVER).Append(HttpHeaders.CRLF);
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
            MemoryStream ms = new MemoryStream(requestBytes);
            /*
             * Keep the index of the byte array, to identify the message body.
             * Step value indicates at what point the parsing algorithm currently is.
             * Step 0 - Method, 1 - URI, 2 - Query, 3 - HTTPVer, 4 - Header, 5 - Value
             */
            int step = 0;
            string headerKey = string.Empty;
            string headerValue = string.Empty;
            while (ms.Position < ms.Length)
            {
                if (step == 0)
                {
                    Method = ParseMethod(ms);
                    step++;
                }
                else if (step == 1)
                {
                    RequestURI = ParseRequestURI(ms);
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
                    RequestQuery = ParseRequestQuery(ms);
                    step++;
                }
                else if (step == 3)
                {
                    ParseHTTPVer(ms);
                    step++;
                }
                else if (step == 4)
                {
                    char c = Convert.ToChar(ms.ReadByte());
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
                else if (step == 5)
                {
                    char c = Convert.ToChar(ms.ReadByte());
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
            if (step < 4)
            {
                throw new IncompleteRequestException($"Incomplete request.",
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
            }
            if (Headers.ContainsHeader(EntityHeaders.ContentLength))
            {
                int remainingBytes = int.Parse(Headers[EntityHeaders.ContentLength]);
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
            /*
             * Trim all trailing null characters left over from SSL encryption.
             */
            this.BodyString = this.BodyString.Trim('\0');
            Complete = true;
            return;
        }
        /// <summary>
        /// Parse the body into a posted from respecting the reference manual.
        /// </summary>
        /// <returns>Dictionary with posted from.</returns>
        private Form GetPostForm()
        {
            if (Headers.ContainsHeader("Content-Type") && Body != null)
            {
                if (Headers["Content-Type"] == "application/x-www-form-urlencoded")
                {
                    /*
                     * Walk through the buffer and get the form contents.
                     * Step 0 - key, 1 - value.
                     */
                    Form form = new Form();
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
                            form.SetValue(formKey, new TextContentType("text/plain", GetValue(Body, ref i)));
                            step--;
                        }
                    }
                    return form;
                }
                else if (Headers["Content-Type"].Contains("multipart/form-data"))
                {
                    return GetMultipartForm();
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
        private Form GetMultipartForm()
        {
            string boundary = this.Headers["Content-Type"].Substring(this.Headers["Content-Type"].IndexOf("=") + 1);
            Form form = new Form();

            int bodyIndex = 0;
            while (true)
            {
                string contentType = "text/plain";

                if (!MatchesTwoHyphens(bodyIndex))
                {
                    throw new InvalidPostFormException("No boundary where expected");
                }
                bodyIndex += 2;

                if (!MatchesString(bodyIndex, boundary))
                {
                    throw new InvalidPostFormException("No boundary where expected");
                }
                bodyIndex += boundary.Length;

                if (!MatchesCRLF(bodyIndex))
                {
                    if (MatchesTwoHyphens(bodyIndex))
                    {
                        /*
                         * Reached the end of the multipart message
                         */
                        break;
                    }
                    throw new InvalidPostFormException("No new line after boundary");
                }
                bodyIndex += 2;

                if (!MatchesString(bodyIndex, "Content-Disposition: form-data"))
                {
                    throw new InvalidPostFormException("No Content-Disposition header");
                }
                bodyIndex += 30;

                Dictionary<string, string> keys = new Dictionary<string, string>();
                while (Body[bodyIndex] == ';')
                {
                    bodyIndex += 2;
                    var keyName = GetMultipartKeyName(bodyIndex);
                    bodyIndex += keyName.Length + 1;
                    var keyNameField = GetMultipartKeyNameField(bodyIndex);
                    bodyIndex += keyNameField.Length + 2;
                    keys[keyName] = keyNameField;
                }

                if (!MatchesCRLF(bodyIndex))
                {
                    throw new InvalidPostFormException("No new line after boundary");
                }
                bodyIndex += 2;

                if (MatchesString(bodyIndex, "Content-Type: "))
                {
                    bodyIndex += 14;
                    contentType = GetContentType(bodyIndex);
                    bodyIndex += contentType.Length + 2;
                }

                if (!MatchesCRLF(bodyIndex))
                {
                    throw new InvalidPostFormException("No new line after boundary");
                }
                bodyIndex += 2;

                var bytes = GetMultipartValue(bodyIndex, boundary);
                if (keys.Keys.Contains("filename"))
                {
                    form.SetValue(keys["name"], new FileContentType(contentType, keys["filename"], bytes));
                }
                else
                {
                    form.SetValue(keys["name"], new TextContentType(contentType, Encoding.UTF8.GetString(bytes)));
                }
                bodyIndex += bytes.Length + 2;
            }

            return form;
        }

        private bool MatchesCRLF(int index)
        {
            if (index + 1 < Body.Length)
            {
                if (Body[index] == HttpHeaders.CRLF[0] && Body[index + 1] == HttpHeaders.CRLF[1])
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private bool MatchesTwoHyphens(int index)
        {
            if (index + 1 < Body.Length)
            {
                if (Body[index] == '-' && Body[index + 1] == '-')
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private bool MatchesString(int index, string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (index + i < Body.Length)
                {
                    if ((char)Body[index + i] != s[i])
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        private string GetMultipartKeyName(int index)
        {
            StringBuilder sb = new StringBuilder();
            while (Body[index] != '=')
            {
                sb.Append((char)Body[index]);
                index++;
            }
            return sb.ToString();
        }
        private string GetMultipartKeyNameField(int index)
        {
            StringBuilder sb = new StringBuilder();
            if (Body[index] != '\"')
            {
                throw new InvalidPostFormException($"Expected [\"] but found {Body[index]}");
            }
            index++;

            while (Body[index] != '\"')
            {
                sb.Append((char)Body[index]);
                index++;
            }
            return sb.ToString();
        }
        private byte[] GetMultipartValue(int bodyIndex, string boundary)
        {
            int startIndex = bodyIndex;
            bool gatheringData = true;
            while (true)
            {
                if (Body[bodyIndex] == '-')
                {
                    /*
                     * Possible boundary detected. Try and see if it matches.
                     */
                    if (Body[bodyIndex + 1] == '-' && MatchesString(bodyIndex + 2, boundary))
                    {
                        break;
                    }
                }
                bodyIndex++;
            }
            byte[] newBytes = new byte[bodyIndex - startIndex - 2];
            Array.Copy(Body, startIndex, newBytes, 0, bodyIndex - startIndex - 2);
            return newBytes;
        }
        private string GetContentType(int bodyIndex)
        {
            StringBuilder sb = new StringBuilder();
            while (!MatchesCRLF(bodyIndex))
            {
                sb.Append((char)Body[bodyIndex]);
                bodyIndex++;
            }
            return sb.ToString();
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
