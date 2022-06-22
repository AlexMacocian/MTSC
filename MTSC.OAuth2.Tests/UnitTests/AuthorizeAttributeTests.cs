using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MTSC.OAuth2.Attributes;
using MTSC.OAuth2.Authorization;

namespace MTSC.OAuth2.Tests.UnitTests
{
    [TestClass]
    public sealed class AuthorizeAttributeTests
    {
        private readonly Mock<ILogger<AuthorizeAttribute>> loggerMock = new();
        private readonly Mock<IAuthorizationProvider> authorizationProviderMock = new();
        private readonly AuthorizeAttribute authorizeAttribute;

        public AuthorizeAttributeTests()
        {
            this.authorizeAttribute = new AuthorizeAttribute(this.authorizationProviderMock.Object, this.loggerMock.Object);
        }
    }
}
