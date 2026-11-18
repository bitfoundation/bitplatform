namespace Bit.BevTools;

/// <summary>Records the requests that reach the loader's HTTP hooks. Implemented by Bit.BevTools.</summary>
internal interface IBevToolsHttpRecorder
{
    /// <summary>
    /// Notes on the request which session issued it. Called at the outer end of a pipeline, where the caller's
    /// synchronization context is still current; a handler further in may have continued on the thread pool.
    /// </summary>
    void CaptureSession(HttpRequestMessage request);

    /// <summary>
    /// Records the request around <paramref name="next"/>. <paramref name="attachedSessionsOnly"/> is true in
    /// <c>IHttpClientFactory</c> pipelines, which a BevTools registered at startup records with handlers of its own:
    /// recording them here too would show every request twice.
    /// </summary>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next, bool attachedSessionsOnly, CancellationToken cancellationToken);
}
