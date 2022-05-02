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
    /// <summary>
    /// Class encapsulating a http request.
    /// </summary>
    public sealed class HttpRequest
    {
        public HttpRequestHeaderDictionary Headers { get; } = new HttpRequestHeaderDictionary();

        /// <summary>
        /// List of cookies.
        /// </summary>
        public List<Cookie> Cookies { get; } = new List<Cookie>();

        public Form Form { get; } = new Form();
        public HttpMethods Method { get; set; }
        public string RequestURI { get; set; }
        public string RequestQuery { get; set; }
        public byte[] Body { get; set; } = new byte[0];
        public string BodyString { get => Encoding.ASCII.GetString(this.Body).Trim('\0'); set => this.Body = Encoding.ASCII.GetBytes(value); }

        public HttpRequest()
        {
        }

        public HttpRequest(byte[] requestBytes)
        {
            this.ParseRequest(requestBytes);
            if(this.Method == HttpMethods.Post)
            {
                this.ParseBodyForm();
            }
        }

        public static HttpRequest FromBytes(byte[] requestBytes)
        {
            return new HttpRequest(requestBytes);
        }

        public byte[] GetPackedRequest()
        {
            return this.BuildRequest();
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
            while(ms.Position < ms.Length)
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
            while(ms.Position < ms.Length)
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
        /// Build the request bytes based on the message contents.
        /// </summary>
        /// <returns>Array of bytes.</returns>
        private byte[] BuildRequest()
        {
            if(this.Method == HttpMethods.Post && this.Form.Count > 0)
            {
                var formData = new List<byte>();
                string boundary;
                if (this.Headers.ContainsHeader("Content-Type"))
                {
                    boundary = this.Headers["Content-Type"].Substring(this.Headers["Content-Type"].IndexOf("=") + 1);
                }
                else
                {
                    boundary = Guid.NewGuid().ToString();
                    this.Headers["Content-Type"] = "multipart/form-data; boundary=" + boundary;
                }

                formData.AddRange(Encoding.UTF8.GetBytes("--" + boundary + HttpHeaders.CRLF));

                foreach(var content in this.Form)
                {
                    if (content.Value is TextContentType) {
                        formData.AddRange(Encoding.UTF8.GetBytes("Content-Disposition: form-data; name=\"" + content.Key + "\"" + HttpHeaders.CRLF +
                            "Content-Type: " + content.Value.ContentType + HttpHeaders.CRLF + HttpHeaders.CRLF + ((TextContentType)content.Value).Value + HttpHeaders.CRLF));
                    }
                    else if(content.Value is FileContentType)
                    {
                        formData.AddRange(Encoding.UTF8.GetBytes("Content-Disposition: form-data; name=\"" + content.Key + "\"; filename=\"" + 
                            ((FileContentType)content.Value).FileName + "\"" + HttpHeaders.CRLF + 
                            "Content-Type: " + content.Value.ContentType + HttpHeaders.CRLF + HttpHeaders.CRLF));
                        formData.AddRange(((FileContentType)content.Value).Data);
                        formData.AddRange(Encoding.UTF8.GetBytes(HttpHeaders.CRLF));
                    }

                    formData.AddRange(Encoding.UTF8.GetBytes("--" + boundary + HttpHeaders.CRLF));
                }

                var prevBytes = this.Body;
                this.Body = new byte[prevBytes.Length + formData.Count];
                Array.Copy(formData.ToArray(), 0, this.Body, 0, formData.Count);
                Array.Copy(prevBytes, 0, this.Body, formData.Count, prevBytes.Length);
            }

            this.Headers[EntityHeaders.ContentLength] = this.Body.Length.ToString();

            var requestString = new StringBuilder();
            requestString.Append(HttpHeaders.Methods[(int)this.Method]).Append(HttpHeaders.SP).Append(this.RequestURI);
            if(!string.IsNullOrWhiteSpace(this.RequestQuery))
            {
                requestString.Append('?').Append(this.RequestQuery);
            }    

            requestString.Append(HttpHeaders.SP).Append(HttpHeaders.HTTPVER).Append(HttpHeaders.CRLF);
            foreach (var header in this.Headers)
            {
                requestString.Append(header.Key).Append(':').Append(HttpHeaders.SP).Append(header.Value).Append(HttpHeaders.CRLF);
            }

            requestString.Append(HttpHeaders.CRLF);
            if (this.Cookies.Count > 0)
            {
                requestString.Append(HttpHeaders.RequestCookieHeader).Append(':').Append(HttpHeaders.SP);
                for (var i = 0; i < this.Cookies.Count; i++)
                {
                    var cookie = this.Cookies[i];
                    requestString.Append(cookie.BuildCookieString());
                    if (i < this.Cookies.Count - 1)
                    {
                        requestString.Append(';');
                    }
                }
            }

            var request = new byte[requestString.Length + (this.Body == null ? 0 : this.Body.Length)];
            var requestBytes = ASCIIEncoding.ASCII.GetBytes(requestString.ToString());
            Array.Copy(requestBytes, 0, request, 0, requestBytes.Length);
            if (this.Body != null)
            {
                Array.Copy(this.Body, 0, request, requestBytes.Length, this.Body.Length);
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
            var ms = new MemoryStream(requestBytes);
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
                        string headerValue = this.ParseHeaderValue(ms);
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

            if(step < 4)
            {
                throw new IncompleteRequestException($"Incomplete request.",
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
            }

            if (this.Headers.ContainsHeader(EntityHeaders.ContentLength))
            {
                var remainingBytes = int.Parse(this.Headers[EntityHeaders.ContentLength]);
                if (remainingBytes <= ms.Length - ms.Position)
                {
                    this.Body = ms.ReadRemainingBytes();
                }
                else
                {
                    throw new IncompleteRequestBodyException($"Incomplete request body. Expected size {remainingBytes} but remaining size is {ms.Length - ms.Position}.",
                        new HttpRequestParsingException("Exception during parsing of http request. Buffer: " + UTF8Encoding.UTF8.GetString(ms.ToArray())));
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
            return;
        }
        /// <summary>
        /// Parse the body into a posted from respecting the reference manual.
        /// </summary>
        /// <returns>Dictionary with posted from.</returns>
        public void ParseBodyForm()
        {
            if (this.Headers.ContainsHeader("Content-Type") && this.Body != null)
            {
                if (this.Headers["Content-Type"] == "application/x-www-form-urlencoded")
                {
                    /*
                     * Walk through the buffer and get the form contents.
                     * Step 0 - key, 1 - value.
                     */
                    var formKey = string.Empty;
                    var step = 0;
                    for (var i = 0; i < this.Body.Length; i++)
                    {
                        if (step == 0)
                        {
                            formKey = this.GetField(this.Body, ref i);
                            step++;
                        }
                        else
                        {
                            this.Form.SetValue(formKey, new TextContentType("text/plain", this.GetValue(this.Body, ref i)));
                            step--;
                        }
                    }

                    return;
                }
                else if (this.Headers["Content-Type"].Contains("multipart/form-data"))
                {
                    this.GetMultipartForm();
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        private Form GetMultipartForm()
        {
            var boundary = this.Headers["Content-Type"].Substring(this.Headers["Content-Type"].IndexOf("=") + 1).Trim('\"');

            var bodyIndex = 0;
            while (true)
            {
                var contentType = "text/plain";

                if (!this.MatchesTwoHyphens(bodyIndex))
                {
                    throw new InvalidPostFormException("No boundary where expected");
                }

                bodyIndex += 2;

                if (!this.MatchesString(bodyIndex, boundary))
                {
                    throw new InvalidPostFormException("No boundary where expected");
                }

                bodyIndex += boundary.Length;

                if (!this.MatchesCRLF(bodyIndex))
                {
                    if (this.MatchesTwoHyphens(bodyIndex))
                    {
                        /*
                         * Reached the end of the multipart message
                         */
                        break;
                    }

                    throw new InvalidPostFormException("No new line after boundary");
                }

                bodyIndex += 2;

                if (!this.MatchesString(bodyIndex, "Content-Disposition: form-data"))
                {
                    throw new InvalidPostFormException("No Content-Disposition header");
                }

                bodyIndex += 30;

                var keys = new Dictionary<string, string>();
                while (this.Body[bodyIndex] == ';')
                {
                    bodyIndex += 2;
                    var keyName = this.GetMultipartKeyName(bodyIndex);
                    bodyIndex += keyName.Length + 1;
                    var keyNameField = this.GetMultipartKeyNameField(bodyIndex);
                    bodyIndex += keyNameField.Length;
                    while(this.Body[bodyIndex] != ';' && this.Body[bodyIndex] != '\r')
                    {
                        bodyIndex++;
                    }

                    keys[keyName] = keyNameField;
                }

                if (!this.MatchesCRLF(bodyIndex))
                {
                    throw new InvalidPostFormException("No new line after boundary");
                }

                bodyIndex += 2;

                if (this.MatchesString(bodyIndex, "Content-Type: "))
                {
                    bodyIndex += 14;
                    contentType = this.GetContentType(bodyIndex);
                    bodyIndex += contentType.Length + 2;
                }

                if (!this.MatchesCRLF(bodyIndex))
                {
                    throw new InvalidPostFormException("No new line after boundary");
                }

                bodyIndex += 2;

                var bytes = this.GetMultipartValue(bodyIndex, boundary);
                if (keys.Keys.Contains("filename"))
                {
                    this.Form.SetValue(keys["name"], new FileContentType(contentType, keys["filename"], bytes));
                }
                else
                {
                    this.Form.SetValue(keys["name"], new TextContentType(contentType, Encoding.UTF8.GetString(bytes)));
                }

                bodyIndex += bytes.Length + 2;
            }

            return this.Form;
        }

        private bool MatchesCRLF(int index)
        {
            if (index + 1 < this.Body.Length)
            {
                if (this.Body[index] == HttpHeaders.CRLF[0] && this.Body[index + 1] == HttpHeaders.CRLF[1])
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
            if (index + 1 < this.Body.Length)
            {
                if (this.Body[index] == '-' && this.Body[index + 1] == '-')
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
            for (var i = 0; i < s.Length; i++)
            {
                if (index + i < this.Body.Length)
                {
                    if ((char)this.Body[index + i] != s[i])
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
            var sb = new StringBuilder();
            while (this.Body[index] != '=')
            {
                sb.Append((char)this.Body[index]);
                index++;
            }

            return sb.ToString();
        }
        private string GetMultipartKeyNameField(int index)
        {
            var sb = new StringBuilder();
            while (this.Body[index] != ';' && this.Body[index] != '\n' && this.Body[index] != '\r')
            {
                sb.Append((char)this.Body[index]);
                index++;
            }

            return sb.ToString().Trim('\"');
        }
        private byte[] GetMultipartValue(int bodyIndex, string boundary)
        {
            var startIndex = bodyIndex;
            while (true)
            {
                if (this.Body[bodyIndex] == '-')
                {
                    /*
                     * Possible boundary detected. Try and see if it matches.
                     */
                    if (this.Body[bodyIndex + 1] == '-' && this.MatchesString(bodyIndex + 2, boundary))
                    {
                        break;
                    }
                }

                bodyIndex++;
            }

            var newBytes = new byte[bodyIndex - startIndex - 2];
            Array.Copy(this.Body, startIndex, newBytes, 0, bodyIndex - startIndex - 2);
            return newBytes;
        }
        private string GetContentType(int bodyIndex)
        {
            var sb = new StringBuilder();
            while (!this.MatchesCRLF(bodyIndex))
            {
                sb.Append((char)this.Body[bodyIndex]);
                bodyIndex++;
            }

            return sb.ToString();
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
