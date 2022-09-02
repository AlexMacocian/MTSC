using System;
using System.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Models
{
    internal sealed class AuthorizationHttpClientWrapper<T>
    {
        private readonly IHttpClient<T> scopedHttpClient;
        private readonly HttpClient httpClient;

        public AuthorizationHttpClientWrapper(IHttpClient<T> scopedHttpClient)
        {
            this.scopedHttpClient = scopedHttpClient;
        }

        public AuthorizationHttpClientWrapper(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent httpContent)
        {
            if (this.scopedHttpClient is not null)
            {
                return await this.scopedHttpClient.PostAsync(url, httpContent);
            }

            if (this.httpClient is not null)
            {
                return await this.httpClient.PostAsync(url, httpContent);
            }

            throw new InvalidOperationException("No usable http client found");
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            if (this.scopedHttpClient is not null)
            {
                return await this.scopedHttpClient.GetAsync(url);
            }

            if (this.httpClient is not null)
            {
                return await this.httpClient.GetAsync(url);
            }

            throw new InvalidOperationException("No usable http client found");
        }

        public void SetAuthorizationHeader(string accessToken)
        {
            if (this.scopedHttpClient is not null)
            {
                this.scopedHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return;
            }

            if (this.httpClient is not null)
            {
                this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return;
            }

            throw new InvalidOperationException("No usable http client found");
        }
    }
}
