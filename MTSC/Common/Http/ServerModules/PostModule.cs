﻿using System;
using System.Collections.Generic;
using System.Text;
using MTSC.Exceptions;
using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Http.ServerModules
{
    public sealed class PostModule : IHttpModule
    {
        private static string urlEncodedHeader = "application/x-www-form-urlencoded";
        private static string multipartHeader = "multipart/form-data";

        public event EventHandler<Dictionary<string, string>> FormReceived;

        #region Private Methods
        private Dictionary<string, string> ParseFormUrlEncoded(HttpRequest request)
        {
            if (!request.Headers.ContainsHeader(HttpMessage.EntityHeaders.ContentLength))
            {
                throw new InvalidPostFormException("Form is missing content-length header!");
            }
            else
            {
                var formData = new Dictionary<string, string>();
                var length = int.Parse(request.Headers[HttpMessage.EntityHeaders.ContentLength]);
                int step = 0, parsedLength = 0;
                var fieldBuilder = new StringBuilder();
                var valueBuilder = new StringBuilder();
                /*
                 * Parse request body to obtain the form data.
                 * Example of a form: field1=value1&field2=value2
                 */
                for (var i = 0; i < request.Body.Length; i++)
                {
                    if (request.Body[i] == '\n')
                    {
                        continue;
                    }
                    else if (step == 0)
                    {
                        parsedLength++;
                        if (request.Body[i] == '=')
                        {
                            step = 1;
                        }
                        else
                        {
                            fieldBuilder.Append((char)request.Body[i]);
                        }
                    }
                    else
                    {
                        parsedLength++;
                        if (request.Body[i] == '&')
                        {
                            step = 0;
                            formData[fieldBuilder.ToString()] = valueBuilder.ToString();
                            fieldBuilder.Clear();
                            valueBuilder.Clear();
                        }
                        else
                        {
                            valueBuilder.Append((char)request.Body[i]);
                            if (parsedLength == length)
                            {
                                formData[fieldBuilder.ToString()] = valueBuilder.ToString();
                            }
                        }
                    }
                }

                return formData;
            }
        }

        private Dictionary<string, string> ParseFormMultipart(HttpRequest request)
        {
            throw new NotImplementedException("Multipart form parsing not implemented yet.");
        }
        #endregion

        #region Interface Implementation
        bool IHttpModule.HandleRequest(ServerSide.Server server, HttpHandler handler, ClientData client, HttpRequest request, ref HttpResponse response)
        {
            if (request.Method == HttpMessage.HttpMethods.Post &&
               request.Headers.ContainsHeader(HttpMessage.EntityHeaders.ContentType))
            {
                if (request.Headers[HttpMessage.EntityHeaders.ContentType].Contains(urlEncodedHeader))
                {
                    var form = this.ParseFormUrlEncoded(request);
                    FormReceived?.Invoke(this, form);
                    server.LogDebug("Received POST form of " + form.Keys.Count + " keys!");
                    return true;
                }
                else if (request.Headers[HttpMessage.EntityHeaders.ContentType].Contains(multipartHeader))
                {
                    var form = this.ParseFormMultipart(request);
                    FormReceived?.Invoke(this, form);
                    server.LogDebug("Received POST form of " + form.Keys.Count + " keys!");
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

        void IHttpModule.Tick(ServerSide.Server server, HttpHandler handler)
        {

        }
        #endregion
    }
}
