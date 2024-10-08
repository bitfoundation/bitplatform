﻿using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class RequestHeadersDelegationHandler(HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);
        request.SetBrowserResponseStreamingEnabled(true);

        if (CultureInfoManager.MultilingualEnabled)
        {
            var cultureName = string.IsNullOrWhiteSpace(CultureInfo.CurrentUICulture.Name) ? "en-US" : CultureInfo.CurrentUICulture.Name;
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(cultureName));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

