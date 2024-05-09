using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public class RequestHeadersDelegationHandler : DelegatingHandler, IDisposable
{
    private readonly Action? _dispose;
    private int? localHttpServerPort;

    public RequestHeadersDelegationHandler(AuthDelegatingHandler handler, IPubSubService pubSubService)
        : base(handler)
    {
        if (AppRenderMode.IsBlazorHybrid)
        {
            _dispose = pubSubService.Subscribe(PubSubMessages.LOCAL_HTTP_SERVER_STARTED, async args =>
            {
                localHttpServerPort = (int)args;
            });
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);
        request.SetBrowserResponseStreamingEnabled(true);

        if (AppRenderMode.MultilingualEnabled)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentCulture.Name));
        }

        if (localHttpServerPort is not null)
        {
            request.Headers.Add(HeaderName.LocalHttpServerPort, localHttpServerPort.ToString()!);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        _dispose?.Invoke();

        base.Dispose(disposing);
    }
}

