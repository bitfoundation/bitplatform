using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Reporting_API">Reporting API</see>
/// (<c>ReportingObserver</c>).
/// </summary>
/// <remarks>
/// Useful for surfacing browser-emitted deprecation, intervention, CSP-violation, and crash
/// reports to your monitoring stack alongside ordinary errors.
/// </remarks>
public class Reporting(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeBrowserReport);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action<BrowserReport[]>> _handlers = new();

    // Per-instance callback reference (see Keyboard): observers are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Reporting>? _dotNetRef;
    private DotNetObjectReference<Reporting> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>ReportingObserver</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.reporting.isSupported");

    /// <summary>
    /// Invoked from JS on each report batch. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeBrowserReport(Guid id, BrowserReport[] reports)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(reports);
    }

    /// <summary>
    /// Subscribes to browser-generated reports. Use the returned <see cref="ButilSubscription"/> to stop.
    /// </summary>
    /// <param name="types">Optional whitelist of report types (e.g. <c>"deprecation"</c>, <c>"intervention"</c>).
    /// Pass null to receive every type.</param>
    /// <param name="buffered">When true, also delivers reports queued before the observer registered.</param>
    [DynamicDependency(nameof(InvokeBrowserReport), typeof(Reporting))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BrowserReport))]
    public async Task<ButilSubscription> Subscribe(Action<BrowserReport[]> handler,
                                                   string[]? types = null,
                                                   bool buffered = true)
    {
        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);
        await js.InvokeVoid("BitButil.reporting.observe", DotNetRef, id, types, buffered);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.reporting.disconnect", id);
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = System.Linq.Enumerable.ToArray(_handlers.Keys);
            _handlers.Clear();
            foreach (var id in ids)
            {
                await js.InvokeVoid("BitButil.reporting.disconnect", id);
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
