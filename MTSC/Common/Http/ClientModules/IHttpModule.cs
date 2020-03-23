using MTSC.Client.Handlers;

namespace MTSC.Common.Http.ClientModules
{
    public interface IHttpModule
    {
        /// <summary>
        /// Handle a response received from the server.
        /// </summary>
        /// <param name="handler">Handler that operates on the response.</param>
        /// <param name="response">Response message.</param>
        /// <returns>True if no other module should operate on the response.</returns>
        bool HandleResponse(IHandler handler, HttpResponse response);
    }
}
