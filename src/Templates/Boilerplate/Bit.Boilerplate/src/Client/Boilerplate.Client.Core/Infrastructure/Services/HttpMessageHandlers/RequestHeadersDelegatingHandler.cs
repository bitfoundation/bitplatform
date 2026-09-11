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

        request.SetBrowserRequestCredentials(request.Options.GetValueOrDefault(RequestOptionNames.ActionName)?.ToString() is nameof(IUserController.UpdateSession) or nameof(IUserController.SignOut)
            ? BrowserRequestCredentials.Include : BrowserRequestCredentials.Omit);
        // `BrowserRequestCredentials.Omit` would prevent server Set-Cookie or Delete-Cookie headers from being processed by the browser.
        // Setting and removing cookies is crucial for pre-rendering scenarios.

        return await base.SendAsync(request, cancellationToken);
    }
}
