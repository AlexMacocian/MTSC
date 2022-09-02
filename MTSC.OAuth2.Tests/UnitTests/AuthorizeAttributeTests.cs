using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MTSC.Common.Http;
using MTSC.OAuth2.Attributes;
using MTSC.OAuth2.Authorization;
using MTSC.OAuth2.Models;
using MTSC.ServerSide;
using Slim;
using Slim.Exceptions;
using System;
using System.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Tests.UnitTests
{
    [TestClass]
    public sealed class AuthorizeAttributeTests
    {
        private const string AccessTokenObject = "AccessTokenObject";
        private const string AuthorizationCodeValue = "someValue";
        private const string AuthorizationCodeKey = "code";
        private const string StateValue = "someState";
        private const string StateKey = "state";
        private const string MaxAgeAttribute = "Max-Age";
        private const string RedirectUri = "RedirectUri";
        private const string OAuthUri = "OAuthUri";
        private const string AccessTokenKey = "JsonWebTokenString";
        private const string AccessTokenValue = "SomeValueHere";
        
        private readonly Mock<ILogger<AuthorizeAttribute>> loggerMock = new();
        private readonly Mock<IAuthorizationProvider> authorizationProviderMock = new();
        private readonly AuthorizeAttribute authorizeAttribute;

        public AuthorizeAttributeTests()
        {
            this.authorizeAttribute = new AuthorizeAttribute(this.authorizationProviderMock.Object, this.loggerMock.Object);
        }

        [TestMethod]
        public void ParameterlessConstructor_DoesNotInject()
        {
            var container = new ServiceManager();
            container.RegisterSingleton<AuthorizeAttribute>();

            var action = () => container.GetService<AuthorizeAttribute>();

            action.Should().Throw<DependencyInjectionException>();
        }

        [TestMethod]
        public void Constructors_PreferresConstructorWithILogger()
        {
            var calledExpectedConstructor = false;
            var container = new ServiceManager();
            container.RegisterSingleton<AuthorizeAttribute>();
            container.RegisterSingleton(sp => this.authorizationProviderMock.Object);
            container.RegisterSingleton(sp =>
            {
                calledExpectedConstructor = true;
                return this.loggerMock.Object;
            });
            container.RegisterSingleton(sp => new Server());

            _ = container.GetService<AuthorizeAttribute>();

            calledExpectedConstructor.Should().BeTrue();
        }

        [TestMethod]
        public void Constructor1_AuthorizationProviderIsNull_ThrowsArgumentNullException()
        {
            var action = () => new AuthorizeAttribute(null, this.loggerMock.Object);

            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor1_ILoggerIsNull_ThrowsArgumentNullException()
        {
            var action = () => new AuthorizeAttribute(this.authorizationProviderMock.Object, null as ILogger<AuthorizeAttribute>);

            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor2_AuthorizationProviderIsNull_ThrowsArgumentNullException()
        {
            var action = () => new AuthorizeAttribute(null, new Server());

            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor2_ServerIsNull_ThrowsArgumentNullException()
        {
            var action = () => new AuthorizeAttribute(this.authorizationProviderMock.Object, null as Server);

            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieExists_VerifiesAccessToken()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            httpRequest.Cookies.Add(new Cookie(AccessTokenKey, AccessTokenValue));
            this.authorizationProviderMock.Setup(u => u.VerifyAccessToken(AccessTokenValue))
                .ReturnsAsync(new TokenValidationResponse { IsValid = true });

            await this.authorizeAttribute.HandleRequestAsync(routeContext);

            this.authorizationProviderMock.Verify();
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieExists_AcceptsRequest()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            httpRequest.Cookies.Add(new Cookie(AccessTokenKey, AccessTokenValue));
            this.authorizationProviderMock.Setup(u => u.VerifyAccessToken(AccessTokenValue))
                .ReturnsAsync(new TokenValidationResponse { IsValid = true });

            var response = await this.authorizeAttribute.HandleRequestAsync(routeContext);

            response.Should().Be(RouteEnablerAsyncResponse.Accept);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieExists_SetsAccessTokenInResources()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            httpRequest.Cookies.Add(new Cookie(AccessTokenKey, AccessTokenValue));
            this.authorizationProviderMock.Setup(u => u.VerifyAccessToken(AccessTokenValue))
                .ReturnsAsync(new TokenValidationResponse { IsValid = true });

            _ = await this.authorizeAttribute.HandleRequestAsync(routeContext);

            routeContext.Resources[AccessTokenKey].Should().Be(AccessTokenValue);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieExists_ReturnsError()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            httpRequest.Cookies.Add(new Cookie(AccessTokenKey, AccessTokenValue));
            this.authorizationProviderMock.Setup(u => u.VerifyAccessToken(AccessTokenValue))
                .ReturnsAsync(new TokenValidationResponse { IsValid = false });

            var response = await this.authorizeAttribute.HandleRequestAsync(routeContext);

            response.Should().BeOfType<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieExists_Redirects()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            httpRequest.Cookies.Add(new Cookie(AccessTokenKey, AccessTokenValue));
            this.authorizationProviderMock.Setup(u => u.VerifyAccessToken(AccessTokenValue))
                .ReturnsAsync(new TokenValidationResponse { IsValid = false });
            this.authorizationProviderMock.Setup(u => u.GetRedirectUri())
                .ReturnsAsync(RedirectUri);

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.Headers.ContainsHeader(HttpMessage.ResponseHeaders.Location).Should().BeTrue();
            response.Response.Headers[HttpMessage.ResponseHeaders.Location].Should().Be(RedirectUri);
            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.TemporaryRedirect);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieExists_ExpiresCookie()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            httpRequest.Cookies.Add(new Cookie(AccessTokenKey, AccessTokenValue));
            this.authorizationProviderMock.Setup(u => u.VerifyAccessToken(AccessTokenValue))
                .ReturnsAsync(new TokenValidationResponse { IsValid = false });
            this.authorizationProviderMock.Setup(u => u.GetRedirectUri())
                .ReturnsAsync(RedirectUri);

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.Cookies.First(c => c.Key == AccessTokenKey).Attributes.First(a => a.Key == MaxAgeAttribute).Value.Should().Be("-1");
            response.Response.Cookies.First(c => c.Key == AccessTokenKey).Value.Should().Be(AccessTokenValue);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_StateKeyMissing_RedirectsToOAuth()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.Headers.ContainsHeader(HttpMessage.ResponseHeaders.Location).Should().BeTrue();
            response.Response.Headers[HttpMessage.ResponseHeaders.Location].Should().Be(OAuthUri);
            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.TemporaryRedirect);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_StateKeyMissing_SetsStateCookie()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.Cookies.First(c => c.Key == StateKey).Value.Should().NotBeNullOrWhiteSpace();
        }


        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_RequestQueryMissing_RedirectsToOAuth()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.Headers.ContainsHeader(HttpMessage.ResponseHeaders.Location).Should().BeTrue();
            response.Response.Headers[HttpMessage.ResponseHeaders.Location].Should().Be(OAuthUri);
            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.TemporaryRedirect);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_RequestQueryMissing_SetsStateCookie()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.Cookies.First(c => c.Key == StateKey).Value.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_NoStateInQuery_ReturnsUnauthorized()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = "someKey=someValue";

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.Unauthorized);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_StatesDoNotMatch_ReturnsUnauthorized()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = $"{StateKey}=someValue";

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.Unauthorized);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_NoAuthCode_ReturnsUnauthorized()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = $"{StateKey}={StateValue}";

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.Unauthorized);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_WithAuthCode_RetrievesAccessToken()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            this.authorizationProviderMock.Setup(u => u.RetrieveAccessToken(AuthorizationCodeValue))
                .ReturnsAsync(Optional.None<JsonWebToken>());
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = $"{StateKey}={StateValue}&{AuthorizationCodeKey}={AuthorizationCodeValue}";

            _ = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            this.authorizationProviderMock.Verify();
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_WrongAuthCode_ReturnsUnauthorized()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            this.authorizationProviderMock.Setup(u => u.RetrieveAccessToken(AuthorizationCodeValue))
                .ReturnsAsync(Optional.None<JsonWebToken>());
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = $"{StateKey}={StateValue}&{AuthorizationCodeKey}={AuthorizationCodeValue}";

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.Unauthorized);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_WithAuthCode_RedirectsToRedirectUri()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            var jsonWebToken = new JsonWebToken(new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken()));
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            this.authorizationProviderMock.Setup(u => u.GetRedirectUri())
                .ReturnsAsync(RedirectUri);
            this.authorizationProviderMock.Setup(u => u.RetrieveAccessToken(AuthorizationCodeValue))
                .ReturnsAsync(jsonWebToken);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = $"{StateKey}={StateValue}&{AuthorizationCodeKey}={AuthorizationCodeValue}";

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.StatusCode.Should().Be(HttpMessage.StatusCodes.TemporaryRedirect);
            response.Response.Headers.ContainsHeader(HttpMessage.ResponseHeaders.Location).Should().BeTrue();
            response.Response.Headers[HttpMessage.ResponseHeaders.Location].Should().Be(RedirectUri);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_WithAuthCode_SetsAccessTokenInCookie()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            var jsonWebToken = new JsonWebToken(new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken()));
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            this.authorizationProviderMock.Setup(u => u.GetRedirectUri())
                .ReturnsAsync(RedirectUri);
            this.authorizationProviderMock.Setup(u => u.RetrieveAccessToken(AuthorizationCodeValue))
                .ReturnsAsync(jsonWebToken);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = $"{StateKey}={StateValue}&{AuthorizationCodeKey}={AuthorizationCodeValue}";

            var response = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            response.Response.Cookies.First(c => c.Key == AccessTokenKey).Value.Should().NotBeNull();
        }

        [TestMethod]
        public async Task HandleRequestAsync_AccessTokenCookieMissing_WithAuthCode_SetsRouteContextResources()
        {
            var httpRequest = new HttpRequest();
            var routeContext = new RouteContext(
                server: null,
                httpRequest,
                clientData: null,
                scopedServiceProvider: null,
                urlValues: null);
            var jsonWebToken = new JsonWebToken(new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken()));
            this.authorizationProviderMock.Setup(u => u.GetOAuthUri(It.IsAny<string>()))
                .ReturnsAsync(OAuthUri);
            this.authorizationProviderMock.Setup(u => u.GetRedirectUri())
                .ReturnsAsync(RedirectUri);
            this.authorizationProviderMock.Setup(u => u.RetrieveAccessToken(AuthorizationCodeValue))
                .ReturnsAsync(jsonWebToken);
            httpRequest.Cookies.Add(new Cookie(StateKey, StateValue));
            httpRequest.RequestQuery = $"{StateKey}={StateValue}&{AuthorizationCodeKey}={AuthorizationCodeValue}";

            _ = (await this.authorizeAttribute.HandleRequestAsync(routeContext)).Cast<RouteEnablerAsyncResponse.RouteEnablerAsyncResponseError>();

            routeContext.Resources.TryGetValue(AccessTokenKey, out var accessTokenString).Should().BeTrue();
            accessTokenString.Should().NotBeNull();
            routeContext.Resources.TryGetValue(AccessTokenObject, out var accessTokenObject).Should().BeTrue();
            accessTokenObject.Should().BeOfType<JsonWebToken>();
        }
    }
}
