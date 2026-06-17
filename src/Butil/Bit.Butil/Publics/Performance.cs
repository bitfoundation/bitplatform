using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance">Performance</see>
/// timing and marker API.
/// </summary>
public class Performance(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokePerformanceObserver);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action<JsonElement[]>> _handlers = new();

    // Per-instance callback reference (see Keyboard): observers are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Performance>? _dotNetRef;
    private DotNetObjectReference<Performance> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// High-resolution timestamp (<c>DOMHighResTimeStamp</c>) since the time origin, in milliseconds.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/now">Performance.now()</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<double> Now()
        => js.Invoke<double>("BitButil.performance.now");

    /// <summary>
    /// The time origin of the document - typically the navigation start, in Unix epoch milliseconds.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/timeOrigin">Performance.timeOrigin</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<double> TimeOrigin()
        => js.Invoke<double>("BitButil.performance.timeOrigin");

    /// <summary>
    /// Adds a named mark to the browser's performance timeline.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/mark">Performance.mark()</see>
    /// </summary>
    public ValueTask Mark(string name) => js.InvokeVoid("BitButil.performance.mark", name);

    /// <summary>
    /// Creates a named measure between two marks (or between a mark and "now").
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/measure">Performance.measure()</see>
    /// </summary>
    public ValueTask Measure(string name, string? startMark = null, string? endMark = null)
        => js.InvokeVoid("BitButil.performance.measure", name, startMark, endMark);

    /// <summary>Removes performance marks. <c>null</c> clears all of them.</summary>
    public ValueTask ClearMarks(string? name = null) => js.InvokeVoid("BitButil.performance.clearMarks", name);

    /// <summary>Removes performance measures. <c>null</c> clears all of them.</summary>
    public ValueTask ClearMeasures(string? name = null) => js.InvokeVoid("BitButil.performance.clearMeasures", name);

    /// <summary>Empties the resource-timing buffer.</summary>
    public ValueTask ClearResourceTimings() => js.InvokeVoid("BitButil.performance.clearResourceTimings");

    /// <summary>
    /// Returns all entries (<c>PerformanceEntry</c>) recorded so far. Optionally filter by name and/or type.
    /// Returned shapes vary by entry type, so we surface them as <see cref="JsonElement"/>.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/getEntries">Performance.getEntries()</see>
    /// </summary>
    public ValueTask<JsonElement[]> GetEntries(string? name = null, string? type = null)
        => js.Invoke<JsonElement[]>("BitButil.performance.getEntries", name, type);

    /// <summary>
    /// Chrome-only memory snapshot. All fields are null on browsers that don't expose <c>performance.memory</c>.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceMemory))]
    public ValueTask<PerformanceMemory> GetMemory()
        => js.Invoke<PerformanceMemory>("BitButil.performance.memory");

    /// <summary>
    /// Invoked from JS on each observer report. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokePerformanceObserver(Guid id, JsonElement[] entries)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(entries);
    }

    /// <summary>
    /// Subscribes to <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceObserver">PerformanceObserver</see>
    /// for one or more entry types. Common values: <c>"resource"</c>, <c>"navigation"</c>,
    /// <c>"longtask"</c>, <c>"largest-contentful-paint"</c>, <c>"layout-shift"</c>,
    /// <c>"first-input"</c>, <c>"paint"</c>, <c>"mark"</c>, <c>"measure"</c>.
    /// </summary>
    /// <param name="buffered">When true, the observer is also notified about entries that
    /// were already in the buffer when the observer registered.</param>
    [DynamicDependency(nameof(InvokePerformanceObserver), typeof(Performance))]
    public async Task<ButilSubscription> SubscribeObserver(string[] entryTypes,
                                                          Action<JsonElement[]> handler,
                                                          bool buffered = true)
    {
        if (entryTypes is null || entryTypes.Length == 0)
            throw new ArgumentException("At least one entry type is required.", nameof(entryTypes));

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);
        await js.InvokeVoid("BitButil.performance.observe", DotNetRef, id, entryTypes, buffered);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.performance.disconnect", id);
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids)
            {
                await js.InvokeVoid("BitButil.performance.disconnect", id);
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
