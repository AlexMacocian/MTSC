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
public sealed class ClientSecretAuthorizationProviderTests
{
    private const string AuthorizationCode = "AuthorizationCode";
    private const string ClientSecret = "clientSecret";

    private readonly ClientSecretAuthorizationProvider provider;
    private readonly AuthorizationOptions authorizationOptions;
    private readonly ScopedHttpClientMock<ClientSecretAuthorizationProvider> httpClientMock = new();
    private readonly HttpClientMock httpClientMock2 = new();

    public ClientSecretAuthorizationProviderTests()
    {
        this.authorizationOptions = new AuthorizationOptions
        {
            ClientCertificate = CertificateUtilities.CreateNewSelfSignedCertificate(),
            OpenIdConfigurationUri = string.Empty,
            OAuthUri = string.Empty,
            AuthenticationMode = AuthorizationOptions.Authentication.ClientSecret,
            AuthTokenEndpoint = string.Empty,
            ClientId = string.Empty,
            ClientSecret = ClientSecret,
            RedirectUri = string.Empty,
            Scopes = string.Empty
        };

        this.provider = new ClientSecretAuthorizationProvider(this.authorizationOptions, this.httpClientMock2);
    }

    [TestMethod]
    public void Constructor_WithIHttpClient_AndOptions_Builds()
    {
        var container = new ServiceManager();
        container.RegisterSingleton<ClientSecretAuthorizationProvider>();
        container.RegisterSingleton<IHttpClient<ClientSecretAuthorizationProvider>>(sp => this.httpClientMock);
        container.RegisterSingleton(sp => this.authorizationOptions);

        var provider = container.GetService<ClientSecretAuthorizationProvider>();

        provider.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithHttpClient_AndOptions_Builds()
    {
        var container = new ServiceManager();
        container.RegisterSingleton<ClientSecretAuthorizationProvider>();
        container.RegisterSingleton<HttpClient>(sp => this.httpClientMock2);
        container.RegisterSingleton(sp => this.authorizationOptions);

        var provider = container.GetService<ClientSecretAuthorizationProvider>();

        provider.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithOptions_Builds()
    {
        var container = new ServiceManager();
        container.RegisterSingleton<ClientSecretAuthorizationProvider>();
        container.RegisterSingleton(sp => this.authorizationOptions);

        var provider = container.GetService<ClientSecretAuthorizationProvider>();

        provider.Should().NotBeNull();
    }

    [TestMethod]
    public async Task RetrieveAccessToken_SetsClientSecret()
    {
        this.httpClientMock2.SendingAsync = async (request) =>
        {
            request.Content.Should().BeOfType<FormUrlEncodedContent>();
            var contentString = await request.Content.As<FormUrlEncodedContent>().ReadAsStringAsync();
            var content = contentString.Split('&');
            var clientSecret = content.Where(s => s.StartsWith("client_secret=")).FirstOrDefault();
            clientSecret.Should().NotBeNull();
            clientSecret?.Replace("client_secret=", "").Should().Be(ClientSecret);

            return new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest
            };
        };

        await this.provider.RetrieveAccessToken(AuthorizationCode);
    }
}
