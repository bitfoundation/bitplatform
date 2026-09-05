using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Browser <c>fetch()</c> wrapper with progress reporting and an abortable handle. Prefer
/// <c>HttpClient</c> for normal API calls; reach for this when you need progress for big
/// downloads or fetch-only features (CORS modes, no-cors, etc.).
/// </summary>
[ButilService(typeof(Fetch))]
public class Fetch(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeFetchProgress);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action<FetchProgress>> _progressHandlers = new();

    // Per-instance callback reference (see Keyboard): progress callbacks are isolated per circuit /
    // WASM app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Fetch>? _dotNetRef;
    private DotNetObjectReference<Fetch> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS as bytes arrive. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeFetchProgress(Guid id, FetchProgress progress)
    {
        if (_progressHandlers.TryGetValue(id, out var handler)) handler.Invoke(progress);
    }

    /// <summary>
    /// Sends the request and returns the full response.
    /// </summary>
    /// <param name="request">The URL, method, headers and body to send.</param>
    /// <param name="onProgress">Optional callback fired as bytes arrive.</param>
    /// <param name="cancellationToken">When triggered, aborts the request.</param>
    [DynamicDependency(nameof(InvokeFetchProgress), typeof(Fetch))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchResponse))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchHeaders))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchProgress))]
    public async Task<FetchResponse> Send(FetchRequest request,
                                          Action<FetchProgress>? onProgress = null,
                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = Guid.NewGuid();
        if (onProgress is not null)
            _progressHandlers.TryAdd(id, onProgress);

        var registration = RegisterAbort(id, cancellationToken);

        try
        {
            return await js.Invoke<FetchResponse>("BitButil.fetch.send",
                cancellationToken,
                id, request, onProgress is not null ? DotNetRef : null, onProgress is not null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The token fired: the JS side was aborted (via the registration above) and produced an
            // aborted response, but awaiting with the token also cancels this .NET task before that
            // response is marshaled back. Honor the documented contract - cancellation yields a
            // FetchResponse with Aborted = true, matching the AbortableFetch.Abort() path - instead
            // of surfacing an exception that callers using the token path wouldn't expect.
            return new FetchResponse { Url = request.Url, Aborted = true, Type = "error" };
        }
        finally
        {
            registration.Dispose();
            _progressHandlers.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// True when the engine can send a request body as a stream. Chromium-only at the time of
    /// writing, and only over HTTP/2 or HTTP/3 in a secure context.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/Request">Request()</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> SupportsStreamingUpload()
        => js.Invoke<bool>("BitButil.fetch.supportsStreamingUpload");

    /// <summary>
    /// Sends the request with <paramref name="body"/> as a streamed upload: the browser pulls from
    /// the .NET stream as the connection drains, so neither side holds the payload whole.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/Request">Request()</see>
    /// </summary>
    /// <param name="request">The URL, method and headers to send. <see cref="FetchRequest.Body"/> is ignored - <paramref name="body"/> is the body.</param>
    /// <param name="body">The stream to upload. The caller owns it and is responsible for disposing it once this returns.</param>
    /// <param name="onProgress">Optional callback fired as bytes are handed to the browser - which runs ahead of what the server has received.</param>
    /// <param name="cancellationToken">When triggered, aborts the request. This returns once the
    /// browser has let go of <paramref name="body"/> - so an aborted call still yields a
    /// <see cref="FetchResponse"/> with <see cref="FetchResponse.Aborted"/> set rather than throwing,
    /// and the stream is safe to dispose the moment it returns.</param>
    /// <remarks>
    /// Check <see cref="SupportsStreamingUpload"/> first: where streaming upload is unsupported the
    /// browser rejects the request outright, which comes back as a failed <see cref="FetchResponse"/>
    /// with <see cref="FetchResponse.Error"/> set rather than as an exception. A streamed body also
    /// cannot be combined with <see cref="FetchRequest.KeepAlive"/>, and a redirect cannot replay
    /// it - use <c>Redirect = "error"</c> if a silent truncation would be worse than a failure.
    /// </remarks>
    [DynamicDependency(nameof(InvokeFetchProgress), typeof(Fetch))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchResponse))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchHeaders))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchProgress))]
    public async Task<FetchResponse> SendStream(FetchRequest request,
                                                Stream body,
                                                Action<FetchProgress>? onProgress = null,
                                                CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(body);

        // Already cancelled: nothing has been posted to JS yet, so aborting it there would have
        // nothing to find. Answer with the same aborted response the abort path produces.
        if (cancellationToken.IsCancellationRequested)
            return new FetchResponse { Url = request.Url, Aborted = true, Type = "error" };

        var id = Guid.NewGuid();
        if (onProgress is not null)
            _progressHandlers.TryAdd(id, onProgress);

        // leaveOpen: the stream is the caller's - they may be writing to it, reusing it, or holding
        // it in a using block of their own, and closing it from under them here would be a surprise.
        using var streamRef = new DotNetStreamReference(body, leaveOpen: true);

        // Only a seekable stream can say how long it is, and the total is only ever a progress
        // label - an unknown one reports null the same way a chunked download does.
        long? total = body.CanSeek ? body.Length - body.Position : null;

        var registration = RegisterAbort(id, cancellationToken);

        try
        {
            // Deliberately not awaited with the token, unlike Send: the browser is pulling from
            // streamRef, and returning the moment the token fires would dispose it - and let the
            // caller dispose their own stream - while a pull is still in flight, which surfaces as
            // an ObjectDisposedException on the interop pump. The registration above aborts the
            // request instead, so the wait ends on the aborted response JS hands back once it has
            // let go of the stream.
            return await js.Invoke<FetchResponse>("BitButil.fetch.sendStream",
                CancellationToken.None,
                id, request, streamRef, onProgress is not null ? DotNetRef : null, onProgress is not null, total);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Same contract as Send: cancellation yields an aborted response rather than throwing.
            return new FetchResponse { Url = request.Url, Aborted = true, Type = "error" };
        }
        finally
        {
            registration.Dispose();
            _progressHandlers.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Wires the token to the JS-side <c>AbortController</c> for this request, or hands back an
    /// empty registration when the token can never fire.
    /// </summary>
    /// <remarks>
    /// This runs before the call is posted to JS, so a token that fires immediately reaches the JS
    /// <c>abort</c> before the request it names exists there. The JS side records such an abort
    /// against the id and the request consumes it as it starts - which is what stops a cancelled
    /// <see cref="SendStream"/> from uploading its body anyway.
    /// </remarks>
    private CancellationTokenRegistration RegisterAbort(Guid id, CancellationToken cancellationToken)
        => cancellationToken.CanBeCanceled
            ? cancellationToken.Register(static state =>
            {
                var (j, rid) = ((IJSRuntime, Guid))state!;
                try { _ = j.InvokeVoid("BitButil.fetch.abort", rid); }
                catch (JSDisconnectedException) { }
            }, (js, id))
            : default;

    /// <summary>
    /// Starts the request and immediately returns an <see cref="AbortableFetch"/> abort handle.
    /// This does not return the response payload - use <see cref="Send"/> for that. Prefer
    /// <see cref="Send"/> unless you only need fire-and-forget abort control.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchHeaders))]
    public async Task<AbortableFetch> Start(FetchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var id = Guid.NewGuid();
        await js.InvokeVoid("BitButil.fetch.start", id, request);
        return new AbortableFetch(js, id);
    }

    /// <summary>Releases the interop reference used for progress callbacks. Requests still in flight are not aborted - use their own <see cref="AbortableFetch"/> for that.</summary>
    public ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
