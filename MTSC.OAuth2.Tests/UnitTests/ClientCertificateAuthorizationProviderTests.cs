using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.OAuth2.Authorization;
using MTSC.OAuth2.Models;
using MTSC.OAuth2.Tests.Models;
using Slim;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Tests.UnitTests;

[TestClass]
public sealed class ClientCertificateAuthorizationProviderTests
{
    private const string AuthorizationCode = "AuthorizationCode";

    private readonly ClientCertificateAuthorizationProvider provider;
    private readonly AuthorizationOptions authorizationOptions;
    private readonly ScopedHttpClientMock<ClientCertificateAuthorizationProvider> httpClientMock = new();
    private readonly HttpClientMock httpClientMock2 = new();

    public ClientCertificateAuthorizationProviderTests()
    {
        this.authorizationOptions = new AuthorizationOptions
        {
            ClientCertificate = CertificateUtilities.CreateNewSelfSignedCertificate(),
            OpenIdConfigurationUri = string.Empty,
            OAuthUri = string.Empty,
            AuthenticationMode = AuthorizationOptions.Authentication.ClientCertificate,
            AuthTokenEndpoint = string.Empty,
            ClientId = string.Empty,
            ClientSecret = string.Empty,
            RedirectUri = string.Empty,
            Scopes = string.Empty
        };

        this.provider = new ClientCertificateAuthorizationProvider(this.httpClientMock2, this.authorizationOptions);
    }

    [TestMethod]
    public void Constructor_WithIHttpClient_AndOptions_Builds()
    {
        var container = new ServiceManager();
        container.RegisterSingleton<ClientCertificateAuthorizationProvider>();
        container.RegisterSingleton<IHttpClient<ClientCertificateAuthorizationProvider>>(sp => this.httpClientMock);
        container.RegisterSingleton(sp => this.authorizationOptions);

        var provider = container.GetService<ClientCertificateAuthorizationProvider>();

        provider.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithHttpClient_AndOptions_Builds()
    {
        var container = new ServiceManager();
        container.RegisterSingleton<ClientCertificateAuthorizationProvider>();
        container.RegisterSingleton<HttpClient>(sp => this.httpClientMock2);
        container.RegisterSingleton(sp => this.authorizationOptions);

        var provider = container.GetService<ClientCertificateAuthorizationProvider>();

        provider.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithOptions_Builds()
    {
        var container = new ServiceManager();
        container.RegisterSingleton<ClientCertificateAuthorizationProvider>();
        container.RegisterSingleton(sp => this.authorizationOptions);

        var provider = container.GetService<ClientCertificateAuthorizationProvider>();

        provider.Should().NotBeNull();
    }

    [TestMethod]
    public async Task RetrieveAccessToken_SetsClientCertificate()
    {
        this.httpClientMock2.SendingAsync = async (request) =>
        {
            request.Content.Should().BeOfType<FormUrlEncodedContent>();
            var contentString = await request.Content.As<FormUrlEncodedContent>().ReadAsStringAsync();
            var content = contentString.Split('&');
            var clientAssertion = content.Where(s => s.StartsWith("client_assertion=")).FirstOrDefault();
            clientAssertion.Should().NotBeNull();

            return new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest
            };
        };

        await this.provider.RetrieveAccessToken(AuthorizationCode);
    }
}
