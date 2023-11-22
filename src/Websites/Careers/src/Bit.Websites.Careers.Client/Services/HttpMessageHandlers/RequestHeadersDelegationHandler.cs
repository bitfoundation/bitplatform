using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Bit.Websites.Careers.Client.Services.HttpMessageHandlers;

public class RequestHeadersDelegationHandler(RetryDelegatingHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserResponseStreamingEnabled(true);

        return await base.SendAsync(request, cancellationToken);
    }
}

