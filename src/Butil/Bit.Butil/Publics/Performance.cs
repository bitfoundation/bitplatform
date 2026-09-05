using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance">Performance</see>
/// timing and marker API.
/// </summary>
[ButilService(typeof(Performance))]
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
    /// Returns the entries of one <c>entryType</c>, deserialized into the type that describes that
    /// kind - <see cref="PerformanceNavigationTiming"/> for <c>"navigation"</c>,
    /// <see cref="LayoutShift"/> for <c>"layout-shift"</c>, and so on. The named getters below are
    /// this method with the pairing already made.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/getEntriesByType">Performance.getEntriesByType()</see>
    /// </summary>
    /// <typeparam name="T">The type describing <paramref name="entryType"/>. A mismatched pairing deserializes to a mostly-default object rather than failing.</typeparam>
    /// <param name="entryType">One of the <see cref="PerformanceEntryTypes"/> constants.</param>
    /// <param name="name">Optionally narrow to entries with this name - a URL, or a mark's name.</param>
    /// <remarks>
    /// The timeline only holds the buffered entry types. Long tasks, long animation frames, layout
    /// shifts, LCP candidates, event timings and element timings are delivered to observers and
    /// never stored there, so for those this reads what Butil's own observer has collected: the
    /// first call starts that observer and comes back empty or nearly so, and a later call returns
    /// what the page produced in between. Call it once early and read it again later - or use
    /// <see cref="SubscribeObserver{T}(string[], Action{T[]}, bool)"/> to be told as they happen.
    /// An entry type the engine does not support stays empty rather than failing.
    /// </remarks>
    public ValueTask<T[]> GetTypedEntries<[DynamicallyAccessedMembers(JsonSerialized)] T>(string entryType, string? name = null)
        => js.Invoke<T[]>("BitButil.performance.getEntries", name, entryType);

    /// <summary>
    /// The document's own load timing - one entry, or none during prerender.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceNavigationTiming">PerformanceNavigationTiming</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceNavigationTiming))]
    public ValueTask<PerformanceNavigationTiming[]> GetNavigationEntries()
        => GetTypedEntries<PerformanceNavigationTiming>(PerformanceEntryTypes.Navigation);

    /// <summary>
    /// Every subresource the document fetched, with its full timing breakdown.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceResourceTiming">PerformanceResourceTiming</see>
    /// </summary>
    /// <param name="name">Optionally narrow to one URL.</param>
    /// <remarks>
    /// The resource buffer holds 250 entries by default and then silently stops recording, so a
    /// long-lived app should either subscribe instead or call <see cref="ClearResourceTimings"/>
    /// after each read.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceResourceTiming))]
    public ValueTask<PerformanceResourceTiming[]> GetResourceEntries(string? name = null)
        => GetTypedEntries<PerformanceResourceTiming>(PerformanceEntryTypes.Resource, name);

    /// <summary>
    /// Tasks that blocked the main thread for more than 50 ms.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceLongTaskTiming">PerformanceLongTaskTiming</see>
    /// </summary>
    /// <remarks>
    /// Long tasks never reach the timeline, so this reads Butil's own observer records: the first
    /// call starts that observer and returns nothing, and a later one returns the long tasks since.
    /// <br/>
    /// Those records are the 250 most recent entries of the type - the same window the platform's
    /// own resource buffer keeps - so read them as you go rather than at the end of a long session.
    /// The observer stops when this service's scope is disposed.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceLongTaskTiming))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceTaskAttributionTiming))]
    public ValueTask<PerformanceLongTaskTiming[]> GetLongTasks()
        => GetTypedEntries<PerformanceLongTaskTiming>(PerformanceEntryTypes.LongTask);

    /// <summary>
    /// Frames that took more than 50 ms to produce, with the scripts that caused them.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceLongAnimationFrameTiming">PerformanceLongAnimationFrameTiming</see>
    /// </summary>
    /// <remarks>
    /// Chromium-only; an engine without it returns an empty array rather than failing. Observer-fed
    /// like <see cref="GetLongTasks"/> - the first call starts collecting, a later one reads it.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceLongAnimationFrameTiming))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceScriptTiming))]
    public ValueTask<PerformanceLongAnimationFrameTiming[]> GetLongAnimationFrames()
        => GetTypedEntries<PerformanceLongAnimationFrameTiming>(PerformanceEntryTypes.LongAnimationFrame);

    /// <summary>
    /// The Largest Contentful Paint candidates recorded so far. The last one is the current LCP.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/LargestContentfulPaint">LargestContentfulPaint</see>
    /// </summary>
    /// <remarks>
    /// Observer-fed like <see cref="GetLongTasks"/>, but with the engine's buffer behind it: the
    /// candidates that painted before the first call are backfilled, on the task after it.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LargestContentfulPaint))]
    public ValueTask<LargestContentfulPaint[]> GetLargestContentfulPaints()
        => GetTypedEntries<LargestContentfulPaint>(PerformanceEntryTypes.LargestContentfulPaint);

    /// <summary>
    /// The layout shifts recorded so far. Sum the ones with <c>HadRecentInput</c> false to get CLS -
    /// or read <see cref="GetWebVitals"/>, which does the session windowing properly.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/LayoutShift">LayoutShift</see>
    /// </summary>
    /// <remarks>
    /// Observer-fed like <see cref="GetLongTasks"/>: shifts from before the first call are
    /// backfilled from the engine's buffer, shifts after it accumulate.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LayoutShift))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LayoutShiftAttribution))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LayoutShiftRect))]
    public ValueTask<LayoutShift[]> GetLayoutShifts()
        => GetTypedEntries<LayoutShift>(PerformanceEntryTypes.LayoutShift);

    /// <summary>
    /// The slow interactions recorded so far - the entries INP is computed from.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceEventTiming">PerformanceEventTiming</see>
    /// </summary>
    /// <param name="firstInputOnly">Read the page's single <c>"first-input"</c> entry instead of every slow interaction.</param>
    /// <remarks>
    /// Observer-fed like <see cref="GetLongTasks"/>, collected from a <c>durationThreshold</c> of
    /// 16 ms rather than the 104 ms default, so short interactions count towards INP too.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceEventTiming))]
    public ValueTask<PerformanceEventTiming[]> GetEventTimings(bool firstInputOnly = false)
        => GetTypedEntries<PerformanceEventTiming>(firstInputOnly ? PerformanceEntryTypes.FirstInput : PerformanceEntryTypes.Event);

    /// <summary>
    /// The Core Web Vitals as they stand right now: LCP, CLS and INP, plus FCP and TTFB.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance_API">Performance API</see>
    /// </summary>
    /// <remarks>
    /// CLS and INP accumulate over the life of the document, so the first call is what starts them
    /// being collected - call it once early (buffered entries from before the call are included) and
    /// read it again later rather than calling it only at the end.
    /// <br/>
    /// A metric the engine does not implement comes back null, which is not the same as a zero score.
    /// <br/>
    /// During prerender/SSR (no JS runtime) there is nothing to read and the result itself is
    /// <c>null</c> rather than an exception - read it from <c>OnAfterRenderAsync</c>, and null-check
    /// it if you read it anywhere a prerender pass can reach.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebVitals))]
    public ValueTask<WebVitals> GetWebVitals()
        => js.Invoke<WebVitals>("BitButil.performance.webVitals");

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
    /// <param name="entryTypes">The entry types to observe. At least one is required.</param>
    /// <param name="handler">Called with each batch of entries the observer receives.</param>
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

    /// <summary>
    /// The typed form of <see cref="SubscribeObserver(string[], Action{JsonElement[]}, bool)"/>:
    /// each batch is deserialized into <typeparamref name="T"/> before the handler sees it. This is
    /// the way to read the entry kinds the timeline never stores - long tasks, layout shifts, LCP
    /// candidates and slow interactions.
    /// </summary>
    /// <typeparam name="T">The type describing the entry kind - <see cref="LayoutShift"/>, <see cref="LargestContentfulPaint"/>, and so on.</typeparam>
    /// <param name="entryTypes">The entry types to observe, from <see cref="PerformanceEntryTypes"/>. At least one is required.</param>
    /// <param name="handler">Called with each batch of entries the observer receives.</param>
    /// <param name="buffered">When true, entries already in the buffer when the observer registered are reported too.</param>
    /// <remarks>
    /// Observing several entry types through one subscription is allowed, but every batch is
    /// deserialized as <typeparamref name="T"/> - so mix types only when one type describes them
    /// all (<see cref="PerformanceEntry"/> always does).
    /// <br/>
    /// An entry type the engine does not support is skipped rather than failing the subscription,
    /// so a handler that is never called is the normal way an unsupported metric presents.
    /// </remarks>
    // T's own members are preserved by its annotation, but the DTOs hanging off it are not reached
    // transitively - and unlike the Get* reads, subscribing is a path that can be the only one an
    // app takes, so the nested types have to be rooted here too or their members deserialize empty.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceTaskAttributionTiming))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PerformanceScriptTiming))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LayoutShiftAttribution))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LayoutShiftRect))]
    public async Task<ButilSubscription> SubscribeObserver<[DynamicallyAccessedMembers(JsonSerialized)] T>(
        string[] entryTypes,
        Action<T[]> handler,
        bool buffered = true)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return await SubscribeObserver(entryTypes, entries => handler(Deserialize<T>(entries)), buffered);
    }

    /// <summary>
    /// The observer relay hands back <see cref="JsonElement"/>s because one subscription can carry
    /// several entry shapes; the typed overload converts them here rather than making every caller
    /// do it. Options are the interop's own (camelCase, case-insensitive), so the conversion matches
    /// what a directly deserialized invoke would have produced.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "T is annotated with LinkerFlags.JsonSerialized at every entry point into this method, so the members System.Text.Json reflects over are preserved.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "The entry types are plain DTOs of primitives, strings and arrays of the same - no generic instantiation is created at runtime.")]
    private static T[] Deserialize<[DynamicallyAccessedMembers(JsonSerialized)] T>(JsonElement[] entries)
    {
        var typed = new T[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            typed[i] = entries[i].Deserialize<T>(EntryJsonOptions)!;
        }

        return typed;
    }

    private static readonly JsonSerializerOptions EntryJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Disconnects every PerformanceObserver started through this instance - including the ones
    /// behind the observer-fed reads such as <see cref="GetLongTasks"/> - and releases its interop
    /// reference.
    /// </summary>
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

            // The observer-fed reads start observers that belong to the module rather than to a
            // subscription, so they are not in the loop above and would otherwise outlive the scope.
            await js.InvokeVoid("BitButil.performance.stopRetained");
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
