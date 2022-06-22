using MTSC.OAuth2.Builders;
using MTSC.ServerSide;

namespace MTSC.OAuth2.Extensions
{
    public static class ServerExtensions
    {
        public static MicrosoftGraphAuthorizationBuilder WithMicrosoftGraphAuthorization(this Server server)
        {
            return new MicrosoftGraphAuthorizationBuilder(server);
        }
    }
}
