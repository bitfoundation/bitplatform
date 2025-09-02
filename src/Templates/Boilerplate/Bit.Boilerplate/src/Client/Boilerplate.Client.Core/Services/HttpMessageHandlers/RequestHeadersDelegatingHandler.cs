﻿using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class RequestHeadersDelegatingHandler(ITelemetryContext telemetryContext, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);
        request.SetBrowserResponseStreamingEnabled(true);

        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        if (request.Headers.UserAgent.Any() is false)
        {
            request.Headers.UserAgent.TryParseAdd(telemetryContext.Platform);
        }

        if (CultureInfoManager.InvariantGlobalization is false && string.IsNullOrEmpty(CultureInfo.CurrentUICulture.Name) is false)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentUICulture.Name));
        }

        var isInternalRequest = request.HasExternalApiAttribute() is false;
        if (isInternalRequest)
        {
            request.Headers.Add("X-App-Version", telemetryContext.AppVersion);
            request.Headers.Add("X-App-Platform", AppPlatform.Type.ToString());
        }
        else
        {
            request.Headers.Remove("X-Origin"); // It gets added by default in Program.Services.cs of Client projects and it might be rejected by some external APIs due to CORS limitations.
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
