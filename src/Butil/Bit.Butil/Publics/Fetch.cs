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
public class Fetch(IJSRuntime js)
{
    /// <summary>
    /// Sends the request and returns the full response.
    /// </summary>
    /// <param name="onProgress">Optional callback fired as bytes arrive.</param>
    /// <param name="cancellationToken">When triggered, aborts the request.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchResponse))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchProgress))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchProgressListenersManager))]
    public async Task<FetchResponse> Send(FetchRequest request,
                                          Action<FetchProgress>? onProgress = null,
                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = Guid.NewGuid();
        if (onProgress is not null)
            FetchProgressListenersManager.AddListener(id, onProgress);

        var registration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(static state =>
            {
                var (j, rid) = ((IJSRuntime, Guid))state!;
                try { _ = j.InvokeVoid("BitButil.fetch.abort", rid); }
                catch (JSDisconnectedException) { }
            }, (js, id))
            : default;

        try
        {
            return await js.Invoke<FetchResponse>("BitButil.fetch.send",
                cancellationToken,
                id, request, FetchProgressListenersManager.InvokeMethodName, onProgress is not null);
        }
        finally
        {
            registration.Dispose();
            FetchProgressListenersManager.RemoveListener(id);
        }
    }

    /// <summary>
    /// Starts the request and immediately returns an <see cref="AbortableFetch"/>. Await
    /// <see cref="AbortableFetch"/> won't give you the response — use this when you only
    /// need fire-and-forget abort control. For typical use prefer <see cref="Send"/>.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FetchRequest))]
    public async Task<AbortableFetch> Start(FetchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var id = Guid.NewGuid();
        await js.InvokeVoid("BitButil.fetch.start", id, request);
        return new AbortableFetch(js, id);
    }
}
