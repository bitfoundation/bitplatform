//+:cnd:noEmit
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

public partial class RequestHeadersDelegatingHandler(ITelemetryContext telemetryContext, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserResponseStreamingEnabled(true);

        request.Version = HttpVersion.Version30;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        // Only a device may describe itself: while prerendering this runs in the web server's process, and the
        // diagnostic page reports back every header it was called with, on an anonymous page.
        if (request.Headers.UserAgent.Any() is false && AppPlatform.IsBlazorHybridOrBrowser)
        {
            request.Headers.UserAgent.TryParseAdd(telemetryContext.Platform);
        }

        if (CultureInfoManager.InvariantGlobalization is false && string.IsNullOrWhiteSpace(CultureInfo.CurrentUICulture.Name) is false)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentUICulture.Name));
        }

        var isInternalRequest = request.HasExternalApiAttribute() is false;
        if (isInternalRequest)
        {
            if (request.Headers.Contains("X-App-Version") is false)
                request.Headers.Add("X-App-Version", telemetryContext.AppVersion);
            if (request.Headers.Contains("X-App-Platform") is false)
                request.Headers.Add("X-App-Platform", AppPlatform.Type.ToString());
        }
        else
        {
            request.Headers.Remove("X-Origin"); // It gets added by default in Program.Services.cs of Client projects and it might be rejected by some external APIs due to CORS limitations.
        }

        // UpdateSession, SignOut and Delete write the access_token cookie pre-rendering reads, so the Blazor WebAssembly app Server.Web
        // serves sends them to its own origin, the host that pre-renders. Every other request goes without cookies.
        var writesAccessTokenCookie = isInternalRequest && AppPlatform.IsBrowser && AppPlatform.IsWasmStandalone is false
            && request.Options.GetValueOrDefault(RequestOptionNames.ActionName)?.ToString() is nameof(IUserController.UpdateSession) or nameof(IUserController.SignOut) or nameof(IUserController.Delete);

        //#if (api == "Standalone")
        if (writesAccessTokenCookie)
        {
            // Server.Web's YARP forwarder answers it by passing it on to Server.Api (See Server.Web's Program.Middlewares.cs).
            request.RequestUri = new Uri(new Uri(request.Headers.GetValues("X-Origin").Single()), request.RequestUri!.PathAndQuery.TrimStart('/'));
        }
        //#endif

        request.SetBrowserRequestCredentials(writesAccessTokenCookie ? BrowserRequestCredentials.Include : BrowserRequestCredentials.Omit);

        return await base.SendAsync(request, cancellationToken);
    }
}
