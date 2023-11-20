using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace BlazorWeb.Client.Services.HttpMessageHandlers;

public class PrepareRequestDelegatingHandler
    : DelegatingHandler
{
    public PrepareRequestDelegatingHandler(AuthDelegatingHandler handler)
        : base(handler)
    {

    }

    public PrepareRequestDelegatingHandler()
    {

    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserResponseStreamingEnabled(true);

#if MultilingualEnabled
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentCulture.Name));
#endif

        return await base.SendAsync(request, cancellationToken);
    }
}

