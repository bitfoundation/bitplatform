namespace Bit.BevTools;

/// <summary>
/// Resolves the session that issued a request, and the component that sent it, at the outer end of the pipeline and
/// passes both, in the request's options, to the <see cref="BevToolsHttpMessageHandler"/> at the inner end.
/// </summary>
internal sealed class BevToolsHttpSessionHandler : DelegatingHandler
{
    internal static readonly HttpRequestOptionsKey<BevToolsSession> SessionKey = new("Bit.BevTools.Session");

    internal static readonly HttpRequestOptionsKey<BevToolsCallSites.CallSite> CallerKey = new("Bit.BevTools.Caller");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Capture(request);
        return base.SendAsync(request, cancellationToken);
    }

    internal static void Capture(HttpRequestMessage request)
    {
        if (!request.Options.TryGetValue(SessionKey, out _) && BevToolsSessionResolver.Resolve() is { } session)
        {
            request.Options.Set(SessionKey, session);

            // Here, not where the request is recorded: a retry or resilience handler in between awaits before it
            // sends, and from then on the component is no longer on the stack - retries included. Once per request,
            // and only for one that will be recorded.
            if (session.IsEnabled && session.IsCapturing && session.Options.TrackHttp && session.Options.CaptureHttpCallers)
            {
                request.Options.Set(CallerKey, BevToolsCallSites.Find(skipFrames: 0));
            }
        }
    }
}
