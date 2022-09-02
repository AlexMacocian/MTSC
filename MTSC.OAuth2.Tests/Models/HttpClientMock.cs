using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Tests.Models
{
    public sealed class HttpClientMock : HttpClient
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> SendingAsync { get;set; }
        public Func<HttpRequestMessage, HttpResponseMessage> Sending { get; set; }

        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.SendingAsync is not null)
            {
                return await this.SendingAsync(request);
            }

            return new HttpResponseMessage();
        }

        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.Sending is not null)
            {
                return this.Sending(request);
            }

            return new HttpResponseMessage();
        }
    }
}
