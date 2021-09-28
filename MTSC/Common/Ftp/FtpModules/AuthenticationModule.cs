using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;

namespace MTSC.Common.Ftp.FtpModules
{
    public class AuthenticationModule : IFtpModule
    {
        public Func<AuthenticationData, bool> ValidateAuthentication { get; set; } = AlwaysAllowed;
        
        public AuthenticationModule WithAuthenticationValidation(Func<AuthenticationData, bool> func)
        {
            this.ValidateAuthentication = func;
            return this;
        }
        bool IFtpModule.HandleRequest(FtpRequest request, ClientData client, FtpHandler handler, Server server)
        {
            if (!client.Resources.TryGetResource<AuthenticationData>(out var authData))
            {
                authData = new AuthenticationData();
                client.Resources.SetResource(authData);
            }

            if (request.Command == FtpRequestCommands.USER)
            {
                authData.Username = request.Arguments[0];
                handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.NeedPassword, Message = "Username OK. Need password!" });
                return true;
            }
            else if(request.Command == FtpRequestCommands.PASS)
            {
                if (!string.IsNullOrWhiteSpace(authData.Username))
                {
                    authData.Password = request.Arguments[0];
                    if (this.ValidateAuthentication.Invoke(authData))
                    {
                        handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.UserLoggedIn, Message = "User logged in!" });
                        authData.Authenticated = true;
                    }
                    else
                    {
                        handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.InvalidUsernameOrPassword, Message = "Invalid username or password!" });
                    }
                }
                else
                {
                    handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.NotLoggedIn, Message = "Username not present. Please provide username!" });
                }

                return true;
            }
            else if(!authData.Authenticated)
            {
                handler.QueueFtpResponse(server, client, new FtpResponse { StatusCode = FtpResponseCodes.NotLoggedIn, Message = "Please log in before issuing other commands!" });
                return true;
            }

            return false;
        }

        private static bool AlwaysAllowed(AuthenticationData payload) => true;
    }
}
