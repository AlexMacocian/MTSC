using System;
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
        ConcurrentDictionary<string, Tuple<byte[], DateTime>> fileCache = new();
        List<string> toRemoveCache = new();
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

                var requestFile = this.rootFolder + request.RequestURI;
                if(requestFile.IsSubPathOf(this.rootFolder) && File.Exists(requestFile))
                {
                    /*
                     * If file is in cache, retrieve it from cache.
                     */
                    byte[] bodyData;
                    if (this.fileCache.ContainsKey(requestFile))
                    {
                        bodyData = this.fileCache[requestFile].Item1;
                    }
                    else
                    {
                        bodyData = File.ReadAllBytes(requestFile);
                    }
                    /*
                     * Insert or update cache with the requested file.
                     */
                    var tuple = new Tuple<byte[], DateTime>(bodyData, DateTime.Now);
                    this.fileCache.AddOrUpdate(requestFile, tuple, (key, oldValue) => oldValue = tuple);
                    /*
                     * Build the response packet.
                     */
                    response.Body = bodyData;
                    response.StatusCode = HttpMessage.StatusCodes.OK;
                    response.Headers[HttpMessage.EntityHeaders.ContentType] = "text/html";
                    if (!this.fileCache.ContainsKey(requestFile))
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
            foreach(var keyValuePair in this.fileCache)
            {
                if((DateTime.Now - keyValuePair.Value.Item2).TotalSeconds > 60)
                {
                    this.toRemoveCache.Add(keyValuePair.Key);
                }
            }

            foreach(var fileName in this.toRemoveCache)
            {
                while (!this.fileCache.TryRemove(fileName, out _)) { };
                server.LogDebug("Removed " + fileName + " from cache.");
            }

            this.toRemoveCache.Clear();
        }

        #endregion
    }
}
