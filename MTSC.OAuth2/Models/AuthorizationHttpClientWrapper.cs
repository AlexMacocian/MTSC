using System;
using System.Http;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Models
{
    internal sealed class AuthorizationHttpClientWrapper<T>
    {
        private readonly HttpClientHandler innerHttpClientHandler;

        private readonly IHttpClient<T> scopedHttpClient;
        private readonly HttpClient httpClient;

        public AuthorizationHttpClientWrapper(IHttpClient<T> scopedHttpClient)
        {
            this.scopedHttpClient = scopedHttpClient;
            var innerHttpClient = this.scopedHttpClient.GetType().GetField("httpClient").GetValue(scopedHttpClient);
            this.innerHttpClientHandler = GetInnerPrivateHttpMessageHandler(innerHttpClient);
        }

        public AuthorizationHttpClientWrapper(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.innerHttpClientHandler = GetInnerPrivateHttpMessageHandler(httpClient);
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

        public void AddClientCertificate(X509Certificate2 x509Certificate2)
        {
            if (this.innerHttpClientHandler is null)
            {
                throw new InvalidOperationException("Could not find an inner http client handler to attach client certificate");
            }

            this.innerHttpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            this.innerHttpClientHandler.ClientCertificates.Add(x509Certificate2);
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

        private static HttpClientHandler GetInnerPrivateHttpMessageHandler(object derivedClass)
        {
            var fieldInfo = derivedClass.GetType().BaseType.GetFields(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.FlattenHierarchy)
                .First(f => f.Name == "_handler");
            var innerMessageHandler = fieldInfo.GetValue(derivedClass);
            if (innerMessageHandler is null)
            {
                var httpClientHandler = new HttpClientHandler();
                fieldInfo.SetValue(derivedClass, httpClientHandler);
                return httpClientHandler;
            }
            else if (innerMessageHandler is HttpClientHandler httpClientHandler)
            {
                return httpClientHandler;
            }
            else if (innerMessageHandler is DelegatingHandler delegatingHandler)
            {
                return RecursivelySearchDelegatingHandlers(delegatingHandler);
            }
            else
            {
                return null;
            }
        }

        private static HttpClientHandler RecursivelySearchDelegatingHandlers(DelegatingHandler delegatingHandler)
        {
            if (delegatingHandler.InnerHandler is null)
            {
                var httpClientHandler = new HttpClientHandler();
                delegatingHandler.InnerHandler = httpClientHandler;
                return httpClientHandler;
            }
            else if (delegatingHandler.InnerHandler is HttpClientHandler httpClientHandler)
            {
                return httpClientHandler;
            }
            else if (delegatingHandler.InnerHandler is DelegatingHandler innerDelegatingHandler)
            {
                return RecursivelySearchDelegatingHandlers(innerDelegatingHandler);
            }

            return null;
        }
    }
}
