using System;
using System.Diagnostics.CodeAnalysis;
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchProgress))]
    public async Task<FetchResponse> Send(FetchRequest request,
                                          Action<FetchProgress>? onProgress = null,
                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A token already cancelled on the way in fires its abort registration before the request is
        // dispatched, and an abort for a request JavaScript has not seen yet has nothing to stop - the
        // request would go out and run to completion. Answering here is what "cancelled" means for it.
        if (cancellationToken.IsCancellationRequested)
            return new FetchResponse { Url = request.Url, Aborted = true };

        var id = Guid.NewGuid();
        if (onProgress is not null)
            _progressHandlers.TryAdd(id, onProgress);

        var registration = js.RegisterJsAbort(cancellationToken, "BitButil.fetch.abort", id);

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
            return new FetchResponse { Url = request.Url, Aborted = true };
        }
        finally
        {
            registration.Dispose();
            _progressHandlers.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Starts the request and immediately returns an <see cref="AbortableFetch"/> abort handle.
    /// This does not return the response payload - use <see cref="Send"/> for that. Prefer
    /// <see cref="Send"/> unless you only need fire-and-forget abort control.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchRequest))]
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
