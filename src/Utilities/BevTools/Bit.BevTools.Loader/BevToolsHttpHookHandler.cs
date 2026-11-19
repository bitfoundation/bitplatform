using Microsoft.Extensions.Http;

namespace Bit.BevTools;

/// <summary>
/// Records the requests of a hand-built <c>HttpClient</c> pipeline in BevTools once BevTools is registered or attached,
/// and passes them through untouched before that, or in a build that leaves Bit.BevTools out. Unlike
/// <c>BevToolsHttpMessageHandler</c> it needs nothing from Bit.BevTools, so an app that lazy loads BevTools can build
/// it into its pipeline at startup. Place it closest to the transport handler: every attempt a retry handler makes is
/// then recorded as the request it is. <c>IHttpClientFactory</c> clients need nothing: the loader adds its own hooks there.
/// </summary>
public sealed class BevToolsHttpHookHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _next;

    public BevToolsHttpHookHandler()
    {
        _next = base.SendAsync;
    }

    public BevToolsHttpHookHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _next = base.SendAsync;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        BevToolsHooks.Http is { } recorder
            ? recorder.SendAsync(request, _next, attachedSessionsOnly: false, cancellationToken)
            : base.SendAsync(request, cancellationToken);
}
