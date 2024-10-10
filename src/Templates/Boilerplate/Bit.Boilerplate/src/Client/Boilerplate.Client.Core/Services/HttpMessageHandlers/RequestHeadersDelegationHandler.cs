using System.Net.Http.Headers;
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
            var cultureName = CultureInfo.CurrentUICulture.Name;
            if (string.IsNullOrWhiteSpace(cultureName))
                cultureName = CultureInfoManager.DefaultCulture.Name;

            if (string.IsNullOrWhiteSpace(cultureName) is false)
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(cultureName));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
