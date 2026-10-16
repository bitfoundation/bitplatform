using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.BevTools;

/// <summary>Why a component rendered. Determined from Activity context and render-batch analysis; see <see cref="BevRenderSample.CauseDetail"/>.</summary>
public enum BevRenderCause
{
    Unknown,
    Initial,
    ParentRender,
    UiEvent,
    Navigation,
    StateHasChanged,
}

/// <summary>One execution of a component's render fragment (BuildRenderTree). Diffing time is tracked per batch, not per component.</summary>
public sealed class BevRenderSample
{
    public required long Sequence { get; init; }

    public required DateTimeOffset At { get; init; }

    public required long StartTicks { get; init; }

    public double DurationMs { get; set; }

    public long BatchId { get; init; }

    public BevRenderCause Cause { get; init; }

    public string? CauseDetail { get; init; }

    /// <summary>Timeline event id of this render.</summary>
    public long EventId { get; set; }

    /// <summary>Timeline event that triggered the render (UI event, navigation, parent render) when known.</summary>
    public long? TriggerEventId { get; init; }

    /// <summary>Component fields that changed since the previous render, when state capture is enabled for the component.</summary>
    public IReadOnlyList<string>? ChangedState { get; set; }

    /// <summary>
    /// Parameters and cascading values whose observable value changed since the previous render ("Name: before → after"),
    /// when state capture is enabled for the component. Empty when they were compared and nothing changed; null when
    /// they were not compared (capture started with this render).
    /// </summary>
    public IReadOnlyList<string>? ChangedParameters { get; set; }

    /// <summary>True when parameter values were compared with the previous render's (an empty list means "compared, nothing changed").</summary>
    public bool ParametersCompared => ChangedParameters is not null;

    /// <summary>
    /// A re-render the parent forced by re-applying parameters whose observable values were identical to the previous
    /// render. Blazor cannot skip it on its own (it only short-circuits for primitive-typed parameters); the component
    /// can, with <c>ShouldRender</c>, or the parent can stop recreating equivalent values. Only decided for renders
    /// where the parameters were compared.
    /// </summary>
    public bool IsWastedRender => Cause == BevRenderCause.ParentRender && ChangedParameters is { Count: 0 };

    public bool Failed { get; set; }
}

/// <summary>Live bookkeeping for one component instance. Mutated only on the renderer's dispatcher; read from anywhere.</summary>
public sealed class BevComponentRecord
{
    private static readonly int CauseCount = Enum.GetValues<BevRenderCause>().Length;
    private readonly RingBuffer<BevRenderSample> _renders;
    private readonly WeakReference<IComponent> _component;
    private readonly int[] _rendersByCause = new int[CauseCount];
    private readonly int[] _baselineRendersByCause = new int[CauseCount];
    private int _renderCount;
    private int _errorCount;
    private int _wastedRenders;
    private int _comparedRenders;
    private int _baselineRenderCount;
    private double _baselineTotalMs;
    private double _maxRenderMsSinceBaseline;

    internal BevComponentRecord(long instanceId, IComponent component, Type type, int maxRenders, bool isBevTools, bool isHidden)
    {
        InstanceId = instanceId;
        _component = new WeakReference<IComponent>(component);
        Type = type;
        TypeName = type.FullName ?? type.Name;
        DisplayName = TypeNames.Short(type);
        FirstRenderTitle = DisplayName + " rendered (first)";
        ReRenderTitle = DisplayName + " re-rendered";
        IsBevTools = isBevTools;
        IsHidden = isHidden;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedTicks = Stopwatch.GetTimestamp();
        _renders = new RingBuffer<BevRenderSample>(maxRenders);
    }

    public long InstanceId { get; }

    /// <summary>
    /// The tracked instance. Held weakly: BevTools must never keep a component (and through it a whole subtree,
    /// its services and its renderer) alive after the application dropped it. Returns null once collected.
    /// </summary>
    public IComponent? Component => _component.TryGetTarget(out var component) ? component : null;

    /// <summary>True while the instance is still reachable; false once the GC reclaimed it.</summary>
    public bool IsAlive => _component.TryGetTarget(out _);

    public Type Type { get; }

    public string TypeName { get; }

    public string DisplayName { get; }

    /// <summary>Pre-built timeline titles. Renders are the hottest event in the system; formatting one string per
    /// render is measurable in both time and GC pressure.</summary>
    internal string FirstRenderTitle { get; }

    internal string ReRenderTitle { get; }

    /// <summary>
    /// True for components that belong to BevTools itself; their cost is counted as overhead, not app activity.
    /// <para>
    /// Set from the type at activation and again from the parent once the hierarchy resolves: the panels render
    /// framework components (<c>Virtualize</c>, <c>CascadingValue</c>, <c>DynamicComponent</c>) that an application
    /// renders too, so the type alone cannot tell whose they are — only the position in the tree can.
    /// </para>
    /// </summary>
    public bool IsBevTools { get; private set; }

    /// <summary>Marks this record — and therefore its own descendants, which resolve their parent later — as BevTools' own.</summary>
    internal void MarkBevToolsOwned() => IsBevTools = true;

    /// <summary>Hidden from the tree (framework/library internals) but still tracked so parent chains stay intact.</summary>
    public bool IsHidden { get; }

    public DateTimeOffset CreatedAt { get; }

    public long CreatedTicks { get; }

    /// <summary>True when the render fragment was wrapped (ComponentBase-derived components only).</summary>
    public bool IsInstrumented { get; internal set; }

    public string? InstrumentationNote { get; internal set; }

    public int RenderCount => Volatile.Read(ref _renderCount);

    public double TotalRenderMs { get; private set; }

    public double LastRenderMs { get; private set; }

    public double MaxRenderMs { get; private set; }

    public DateTimeOffset? LastRenderAt { get; private set; }

    public long LastRenderTicks { get; private set; }

    public int ErrorCount => Volatile.Read(ref _errorCount);

    /// <summary>
    /// Renders forced by the parent whose parameters were observably identical to the previous render (see
    /// <see cref="BevRenderSample.IsWastedRender"/>). Only counted while the component's state is captured, i.e. while
    /// it is selected in the inspector or when <see cref="BitBevToolsOptions.CaptureComponentStateOnRender"/> is on.
    /// </summary>
    public int ParentRendersWithUnchangedParameters => Volatile.Read(ref _wastedRenders);

    /// <summary>
    /// Recorded renders whose parameters were compared with the previous render (<see cref="BevRenderSample.ParametersCompared"/>):
    /// the denominator for <see cref="ParentRendersWithUnchangedParameters"/>. Both are session-long counters, unlike
    /// <see cref="Renders"/>, which keeps only the most recent samples.
    /// </summary>
    public int RendersWithComparedParameters => Volatile.Read(ref _comparedRenders);

    /// <summary>
    /// Recorded renders attributed to <paramref name="cause"/>, over the whole session or since <see cref="MarkBaseline"/>.
    /// Counted as renders happen, so the breakdown always adds up to <see cref="RenderCount"/> (or
    /// <see cref="RendersSinceBaseline"/>), however many samples <see cref="Renders"/> still holds.
    /// </summary>
    public int RendersByCause(BevRenderCause cause, bool sinceBaseline = false)
    {
        var index = (int)cause;
        if ((uint)index >= (uint)CauseCount)
        {
            return 0;
        }

        var total = Volatile.Read(ref _rendersByCause[index]);
        return sinceBaseline ? total - Volatile.Read(ref _baselineRendersByCause[index]) : total;
    }

    /// <summary>When the profiler baseline was last reset, or null when statistics cover the whole session.</summary>
    public DateTimeOffset? BaselineAt { get; private set; }

    /// <summary>Renders since <see cref="MarkBaseline"/>; equals <see cref="RenderCount"/> when no baseline was set.</summary>
    public int RendersSinceBaseline => RenderCount - Volatile.Read(ref _baselineRenderCount);

    public double RenderMsSinceBaseline => TotalRenderMs - _baselineTotalMs;

    public double MaxRenderMsSinceBaseline => _maxRenderMsSinceBaseline;

    /// <summary>
    /// Starts a new profiling window: the "since baseline" figures restart from zero while the raw counters keep
    /// running, so render-cause detection (which relies on <see cref="RenderCount"/>) is unaffected.
    /// </summary>
    internal void MarkBaseline(DateTimeOffset at)
    {
        Volatile.Write(ref _baselineRenderCount, RenderCount);
        for (var i = 0; i < CauseCount; i++)
        {
            Volatile.Write(ref _baselineRendersByCause[i], Volatile.Read(ref _rendersByCause[i]));
        }

        _baselineTotalMs = TotalRenderMs;
        _maxRenderMsSinceBaseline = 0;
        BaselineAt = at;
    }

    public bool IsDisposed { get; internal set; }

    public DateTimeOffset? DisposedAt { get; internal set; }

    /// <summary>Renderer-assigned component id, resolved lazily.</summary>
    public int? ComponentId { get; internal set; }

    public BevComponentRecord? Parent { get; internal set; }

    public long? ParentInstanceId => Parent?.InstanceId;

    internal bool ParentResolved { get; set; }

    private WeakReference<Renderer>? _renderer;

    /// <summary>The renderer that owns the component, held weakly for the same reason as <see cref="Component"/>.</summary>
    internal Renderer? Renderer
    {
        get => _renderer is not null && _renderer.TryGetTarget(out var renderer) ? renderer : null;
        set => _renderer = value is null ? null : new WeakReference<Renderer>(value);
    }

    internal bool RendererResolved { get; set; }

    internal bool JsRuntimePatched { get; set; }

    /// <summary>When true, fields are snapshotted before every render so the inspector can show what changed.</summary>
    public bool CaptureState { get; set; }

    internal Dictionary<string, string>? LastStateSnapshot { get; set; }

    /// <summary>Flattened parameter values at the previous captured render, used to tell a forced re-render from a needed one.</summary>
    internal Dictionary<string, string>? LastParameterSnapshot { get; set; }

    public BevRenderSample? LastRender => _renders.Last;

    public BevRenderSample[] Renders => _renders.ToArray();

    /// <summary>Releases everything that could keep application objects alive once the component is gone.</summary>
    internal void ReleaseReferences()
    {
        _component.SetTarget(null!);
        _renderer = null;
        LastStateSnapshot = null;
        LastParameterSnapshot = null;
    }

    internal void AddRender(BevRenderSample sample)
    {
        Interlocked.Increment(ref _renderCount);
        TotalRenderMs += sample.DurationMs;
        LastRenderMs = sample.DurationMs;
        if (sample.DurationMs > MaxRenderMs)
        {
            MaxRenderMs = sample.DurationMs;
        }

        if (sample.DurationMs > _maxRenderMsSinceBaseline)
        {
            _maxRenderMsSinceBaseline = sample.DurationMs;
        }

        if ((uint)sample.Cause < (uint)CauseCount)
        {
            Interlocked.Increment(ref _rendersByCause[(int)sample.Cause]);
        }

        if (sample.ParametersCompared)
        {
            Interlocked.Increment(ref _comparedRenders);
        }

        if (sample.IsWastedRender)
        {
            Interlocked.Increment(ref _wastedRenders);
        }

        LastRenderAt = sample.At;
        LastRenderTicks = sample.StartTicks;
        _renders.Add(sample);
    }

    internal void AddError() => Interlocked.Increment(ref _errorCount);

    private int _skippedRenders;

    /// <summary>Renders that happened while capture was paused. They still count as "has rendered" for cause detection.</summary>
    public int SkippedRenders => Volatile.Read(ref _skippedRenders);

    /// <summary>True once the instance has rendered at least once, recorded or not.</summary>
    internal bool HasRendered => RenderCount > 0 || SkippedRenders > 0;

    internal void NoteSkippedRender() => Interlocked.Increment(ref _skippedRenders);

    public int Depth
    {
        get
        {
            var depth = 0;
            var current = Parent;
            while (current is not null && depth < 100_000)
            {
                depth++;
                current = current.Parent;
            }

            return depth;
        }
    }

    public BevComponentSnapshot ToSnapshot() => new(
        InstanceId, TypeName, DisplayName, ParentInstanceId, Depth, RenderCount, TotalRenderMs, LastRenderMs, MaxRenderMs,
        CreatedAt, LastRenderAt, IsDisposed, ErrorCount, IsHidden, ParentRendersWithUnchangedParameters);

    public override string ToString() => $"{DisplayName}#{InstanceId}";
}

internal static class TypeNames
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, string> Cache = new();

    public static string Short(Type type) => Cache.GetOrAdd(type, static t => Format(t));

    private static string Format(Type t)
    {
        if (t.IsGenericType)
        {
            var name = t.Name;
            var tick = name.IndexOf('`');
            if (tick > 0)
            {
                name = name[..tick];
            }

            return name + "<" + string.Join(", ", t.GetGenericArguments().Select(Format)) + ">";
        }

        if (t.IsArray)
        {
            return Format(t.GetElementType()!) + "[]";
        }

        return t.Name;
    }
}
