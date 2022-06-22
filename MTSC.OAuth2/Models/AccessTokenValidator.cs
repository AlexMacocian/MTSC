using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MTSC.OAuth2.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Models
{
    internal sealed class AccessTokenValidator<T>
    {
        private readonly OpenIdConfigurationCache openIdConfigurationCache;
        private readonly string clientId;
        private readonly AuthorizationHttpClientWrapper<T> authorizationHttpClientWrapper;

        public AccessTokenValidator(
            AuthorizationHttpClientWrapper<T> authorizationHttpClientWrapper,
            string clientId,
            string openIdConfigurationUri)
        {
            this.authorizationHttpClientWrapper = authorizationHttpClientWrapper;
            this.clientId = clientId;
            this.openIdConfigurationCache = OpenIdConfigurationCache.GetForConfigurationUri(openIdConfigurationUri);
        }

        public async Task<Result<bool, TokenValidationException>> ValidateAccessToken(string accessToken)
        {
            var openIdConfiguration = await this.openIdConfigurationCache.GetOpenIdConfiguration(this.authorizationHttpClientWrapper);

            try
            {
                var validationResult = await this.ValidateTokenInternal(accessToken, openIdConfiguration);
                if (validationResult.Exception is Exception validationException)
                {
                    return new TokenValidationException(validationException);
                }

                return validationResult.IsValid;
            }
            catch(Exception ex)
            {
                return new TokenValidationException(ex);
            }
        }

        private async Task<TokenValidationResult> ValidateTokenInternal(string accessToken, OpenIdConfiguration openIdConfiguration)
        {
            var jsonWebTokenHandler = new JsonWebTokenHandler();
            var token = new JsonWebToken(accessToken);
            TokenValidationParameters validationParameters;
            if (token.Audiences.Any(audience => audience == this.clientId))
            {
                validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateAudience = true,
                    ValidAlgorithms = openIdConfiguration.SupportedAlgorithms,
                    ValidAudience = clientId,
                    ValidIssuer = openIdConfiguration.Issuer,
                    IssuerSigningKeys = openIdConfiguration.SigningKeys,
                    ValidateTokenReplay = true,
                };
            }
            else
            {
                validationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateActor = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, parameters) =>
                    {
                        var jwt = new JsonWebToken(accessToken);
                        return jwt;
                    }
                };
            }

            var validation = await jsonWebTokenHandler.ValidateTokenAsync(accessToken, validationParameters);
            return validation;
        }
    }
}
