using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Tests.Models
{
    public sealed class ScopedHttpClientMock<T> : IHttpClient<T>
    {
        private readonly HttpClient httpClient = new();

        public Uri BaseAddress { get; set; }
        public HttpRequestHeaders DefaultRequestHeaders { get; }
        public long MaxResponseContentBufferSize { get; set; }
        public TimeSpan Timeout { get; set; }

        public event EventHandler<HttpClientEventMessage> EventEmitted;

        public void CancelPendingRequests()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetByteArrayAsync(string requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetByteArrayAsync(Uri requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetStreamAsync(string requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetStreamAsync(Uri requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetStringAsync(string requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetStringAsync(Uri requestUri)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
