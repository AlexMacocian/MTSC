﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Http.ServerModules
{
    public sealed class FileServerModule : IHttpModule
    {
        private string rootFolder;
        ConcurrentDictionary<string, Tuple<byte[], DateTime>> fileCache = new ConcurrentDictionary<string, Tuple<byte[], DateTime>>();
        List<string> toRemoveCache = new List<string>();
        public FileServerModule(string rootFolder = "src")
        {
            this.rootFolder = Path.GetFullPath(rootFolder);
        }
        #region Interface Implementation
        bool IHttpModule.HandleRequest(ServerSide.Server server, HttpHandler handler, ClientData client, HttpRequest request, ref HttpResponse response)
        {
            if(request.Method == HttpMessage.HttpMethods.Get)
            {
                if(request.RequestURI == "/")
                {
                    request.RequestURI = "/index.html";
                }
                string requestFile = this.rootFolder + request.RequestURI;
                if(requestFile.IsSubPathOf(this.rootFolder) && File.Exists(requestFile))
                {
                    /*
                     * If file is in cache, retrieve it from cache.
                     */
                    byte[] bodyData;
                    if (fileCache.ContainsKey(requestFile))
                    {
                        bodyData = fileCache[requestFile].Item1;
                    }
                    else
                    {
                        bodyData = File.ReadAllBytes(requestFile);
                    }
                    /*
                     * Insert or update cache with the requested file.
                     */
                    Tuple<byte[], DateTime> tuple = new Tuple<byte[], DateTime>(bodyData, DateTime.Now);
                    fileCache.AddOrUpdate(requestFile, tuple, (key, oldValue) => oldValue = tuple);
                    /*
                     * Build the response packet.
                     */
                    response.Body = bodyData;
                    response.StatusCode = HttpMessage.StatusCodes.OK;
                    response.Headers[HttpMessage.EntityHeaders.ContentType] = "text/html";
                    if (!fileCache.ContainsKey(requestFile))
                    {
                        server.LogDebug("Adding " + requestFile + " to server cache.");
                    }
                    return true;
                }
                else
                {
                    server.LogDebug("File not found: " + requestFile);
                    response.StatusCode = HttpMessage.StatusCodes.NotFound;
                }
            }
            return false;
        }

        void IHttpModule.Tick(ServerSide.Server server, HttpHandler handler)
        {
            foreach(KeyValuePair<string, Tuple<byte[], DateTime>> keyValuePair in fileCache)
            {
                if((DateTime.Now - keyValuePair.Value.Item2).TotalSeconds > 60)
                {
                    toRemoveCache.Add(keyValuePair.Key);
                }
            }
            foreach(string fileName in toRemoveCache)
            {
                Tuple<byte[], DateTime> tp;
                while(!fileCache.TryRemove(fileName, out tp)) { };
                server.LogDebug("Removed " + fileName + " from cache.");
            }
            toRemoveCache.Clear();
        }

        #endregion
    }
}
