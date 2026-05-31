using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance">Performance</see>
/// timing and marker API.
/// </summary>
public class Performance(IJSRuntime js)
{
    /// <summary>
    /// High-resolution timestamp (<c>DOMHighResTimeStamp</c>) since the time origin, in milliseconds.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/now">Performance.now()</see>
    /// </summary>
    public ValueTask<double> Now()
        => js.Invoke<double>("BitButil.performance.now");

    /// <summary>
    /// The time origin of the document — typically the navigation start, in Unix epoch milliseconds.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/timeOrigin">Performance.timeOrigin</see>
    /// </summary>
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
    /// Subscribes to <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceObserver">PerformanceObserver</see>
    /// for one or more entry types. Common values: <c>"resource"</c>, <c>"navigation"</c>,
    /// <c>"longtask"</c>, <c>"largest-contentful-paint"</c>, <c>"layout-shift"</c>,
    /// <c>"first-input"</c>, <c>"paint"</c>, <c>"mark"</c>, <c>"measure"</c>.
    /// </summary>
    /// <param name="buffered">When true, the observer is also notified about entries that
    /// were already in the buffer when the observer registered.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceObserverListenersManager))]
    public async Task<ButilSubscription> SubscribeObserver(string[] entryTypes,
                                                          Action<JsonElement[]> handler,
                                                          bool buffered = true)
    {
        if (entryTypes is null || entryTypes.Length == 0)
            throw new ArgumentException("At least one entry type is required.", nameof(entryTypes));

        var id = PerformanceObserverListenersManager.AddListener(handler);
        await js.InvokeVoid("BitButil.performance.observe",
            PerformanceObserverListenersManager.InvokeMethodName,
            id,
            entryTypes,
            buffered);

        return new ButilSubscription(id, async () =>
        {
            PerformanceObserverListenersManager.RemoveListener(id);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.performance.disconnect", id);
        });
    }
}
