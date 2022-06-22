# OAuth2 Web Server App Implementation

## Setup
* Decorate your `HttpRouteBase` with `[Authorize]`
* Setup server wtih an `IAuthorizationProvider`. Alternatively use `server.WithMicrosoftGraphAuthorization()` extension to use the in-built Microsoft Graph authorization provider.
* [Optionally] implement `IHttpClient<T>` resolver to have a scoped `HttpClient` for the OAuth flow. If no `IHttpClient<T>` is present, the flow will use a `new HttpClient()` instance.
* [Optionally] implement `ILogger<T>` resolver to have a scoped `ILogger` for the OAuth flow. If no `ILogger<T>` is present, the flow will use `Server.LogDebug()`.

## Functionality
* On the first request arriving at an endpoint decorated with `[Authorize]`, the route filter with check for the presence of `AccessToken` cookie. If it is not present, the filter will redirect the client to the OAuth server to peform the authorization.
* The OAuth server redirects the client, on a successful authorization, to the `RedirectUri`. The `RedirectUri` endpoint must also be decorated with `[Authorize]`.
* The redirect endpoint picks up the authorization code from the OAuth server and performs a request to the OAuth server to retrieve the `AccessToken`. The filter will use the `ClientSecret` as identifier of your Web Server App to the OAuth server.
* Once the OAuth server returns the `AccessToken`, the filter will set a `RouteContext.Resources` resource with the key `AccessToken` and value of the code provided by the OAuth server.
* On response from the Web Server App, the filter will also set a response cookie with the `AccessToken`.