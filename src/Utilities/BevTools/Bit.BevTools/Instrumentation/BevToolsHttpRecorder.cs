namespace Bit.BevTools;

/// <summary>Records what reaches the HTTP hooks of Bit.BevTools.Loader, the same way <see cref="BevToolsHttpMessageHandler"/> does.</summary>
internal sealed class BevToolsHttpRecorder : IBevToolsHttpRecorder
{
    public static readonly BevToolsHttpRecorder Instance = new();

    public void CaptureSession(HttpRequestMessage request) => BevToolsHttpSessionHandler.Capture(request);

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next, bool attachedSessionsOnly, CancellationToken cancellationToken)
    {
        var session = BevToolsHttpMessageHandler.ResolveSession(request);
        return session is null || !session.IsEnabled || !session.Options.TrackHttp || (attachedSessionsOnly && !session.IsAttachedAtRuntime)
            ? next(request, cancellationToken)
            : BevToolsHttpMessageHandler.RecordAsync(session, session.Options, request, next, cancellationToken);
    }
}
