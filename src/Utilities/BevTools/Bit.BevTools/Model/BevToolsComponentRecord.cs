using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.BevTools;

/// <summary>Live bookkeeping for one component instance. Mutated only on the renderer's dispatcher; read from anywhere.</summary>
public sealed class BevToolsComponentRecord
{
    private static readonly int CauseCount = Enum.GetValues<BevToolsRenderCause>().Length;
    private readonly BevToolsRingBuffer<BevToolsRenderSample> _renders;
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
    private long _baselineAllocatedBytes;
    private long _maxAllocatedBytesSinceBaseline;

    internal BevToolsComponentRecord(long instanceId, IComponent component, Type type, int maxRenders, bool isBevTools, bool isHidden)
    {
        InstanceId = instanceId;
        _component = new WeakReference<IComponent>(component);
        Type = type;
        TypeName = type.FullName ?? type.Name;
        DisplayName = BevToolsTypeNames.Short(type);
        FirstRenderTitle = DisplayName + " rendered (first)";
        ReRenderTitle = DisplayName + " re-rendered";
        IsBevTools = isBevTools;
        IsHidden = isHidden;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedTicks = Stopwatch.GetTimestamp();
        _renders = new BevToolsRingBuffer<BevToolsRenderSample>(maxRenders);
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

    /// <summary>
    /// True while <see cref="IsBevTools"/> is only a guess from the type: a registered panel's component, which the
    /// application may render on its own pages too. The parent settles it once the hierarchy resolves.
    /// </summary>
    internal bool IsOwnerProvisional { get; private set; }

    /// <summary>Whether the "created" event activation held back, while the owner was provisional, is still to be recorded.</summary>
    internal bool IsCreatedEventPending { get; private set; }

    internal void MarkOwnerProvisional(bool createdEventPending)
    {
        IsOwnerProvisional = true;
        IsCreatedEventPending = createdEventPending;
    }

    /// <summary>Settles a provisional owner: BevTools' when rendered under BevTools' UI, the application's otherwise.</summary>
    internal void SettleOwner(bool isBevTools)
    {
        IsOwnerProvisional = false;
        IsBevTools = isBevTools;
        if (isBevTools)
        {
            IsCreatedEventPending = false;
        }
    }

    internal void ClearCreatedEventPending() => IsCreatedEventPending = false;

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

    /// <summary>Bytes all its recorded renders allocated (<see cref="BevToolsRenderSample.AllocatedBytes"/>); zero where they are not measured.</summary>
    public long TotalAllocatedBytes { get; private set; }

    public long MaxAllocatedBytes { get; private set; }

    public DateTimeOffset? LastRenderAt { get; private set; }

    /// <summary>When the first recorded render happened; it stays when the render history has long since moved on.</summary>
    public DateTimeOffset? FirstRenderAt { get; private set; }

    public long LastRenderTicks { get; private set; }

    /// <summary>
    /// The render mode the renderer assigned to this instance, as written in markup ("InteractiveServer",
    /// "InteractiveWebAssembly, no prerendering"): <c>@rendermode</c> on it or on the component frame that created it,
    /// or <c>[RenderMode*]</c> on its type. Null when none was assigned, which is every component below the one that
    /// was: a render mode applies to a subtree, and BevTools shows where each subtree starts. Read on the first render.
    /// </summary>
    public string? RenderModeName { get; internal set; }

    public int ErrorCount => Volatile.Read(ref _errorCount);

    /// <summary>
    /// Renders forced by the parent whose parameters were observably identical to the previous render (see
    /// <see cref="BevToolsRenderSample.IsWastedRender"/>). Only counted while the component's state is captured, i.e. while
    /// it is selected in the inspector or when <see cref="BevToolsOptions.CaptureComponentStateOnRender"/> is on.
    /// </summary>
    public int ParentRendersWithUnchangedParameters => Volatile.Read(ref _wastedRenders);

    /// <summary>
    /// Recorded renders whose parameters were compared with the previous render (<see cref="BevToolsRenderSample.ParametersCompared"/>):
    /// the denominator for <see cref="ParentRendersWithUnchangedParameters"/>. Both are session-long counters, unlike
    /// <see cref="Renders"/>, which keeps only the most recent samples.
    /// </summary>
    public int RendersWithComparedParameters => Volatile.Read(ref _comparedRenders);

    /// <summary>
    /// Recorded renders attributed to <paramref name="cause"/>, over the whole session or since <see cref="MarkBaseline"/>.
    /// Counted as renders happen, so the breakdown always adds up to <see cref="RenderCount"/> (or
    /// <see cref="RendersSinceBaseline"/>), however many samples <see cref="Renders"/> still holds.
    /// </summary>
    public int RendersByCause(BevToolsRenderCause cause, bool sinceBaseline = false)
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

    public long AllocatedBytesSinceBaseline => TotalAllocatedBytes - _baselineAllocatedBytes;

    public long MaxAllocatedBytesSinceBaseline => _maxAllocatedBytesSinceBaseline;

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
        _baselineAllocatedBytes = TotalAllocatedBytes;
        _maxAllocatedBytesSinceBaseline = 0;
        BaselineAt = at;
    }

    public bool IsDisposed { get; internal set; }

    public DateTimeOffset? DisposedAt { get; internal set; }

    /// <summary>Renderer-assigned component id, resolved lazily.</summary>
    public int? ComponentId { get; internal set; }

    public BevToolsComponentRecord? Parent { get; internal set; }

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

    public BevToolsRenderSample? LastRender => _renders.Last;

    public BevToolsRenderSample[] Renders => _renders.ToArray();

    /// <summary>Releases everything that could keep application objects alive once the component is gone.</summary>
    internal void ReleaseReferences()
    {
        _component.SetTarget(null!);
        _renderer = null;
        LastStateSnapshot = null;
        LastParameterSnapshot = null;
    }

    internal void AddRender(BevToolsRenderSample sample)
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

        if (sample.AllocatedBytes is { } allocated)
        {
            TotalAllocatedBytes += allocated;
            if (allocated > MaxAllocatedBytes)
            {
                MaxAllocatedBytes = allocated;
            }

            if (allocated > _maxAllocatedBytesSinceBaseline)
            {
                _maxAllocatedBytesSinceBaseline = allocated;
            }
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
        FirstRenderAt ??= sample.At;
        LastRenderTicks = sample.StartTicks;
        _renders.Add(sample);
    }

    internal void AddError() => Interlocked.Increment(ref _errorCount);

    private int _skippedRenders;

    /// <summary>Renders that happened while capture was paused. They still count as "has rendered" for cause detection.</summary>
    public int SkippedRenders => Volatile.Read(ref _skippedRenders);

    internal bool RenderedBeforeAttach { get; set; }

    /// <summary>True once the instance has rendered at least once, recorded or not.</summary>
    internal bool HasRendered => RenderCount > 0 || SkippedRenders > 0 || RenderedBeforeAttach;

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

    public BevToolsComponentSnapshot ToSnapshot() => new(
        InstanceId, TypeName, DisplayName, ParentInstanceId, Depth, RenderCount, TotalRenderMs, LastRenderMs, MaxRenderMs,
        CreatedAt, LastRenderAt, IsDisposed, ErrorCount, IsHidden, ParentRendersWithUnchangedParameters);

    public override string ToString() => $"{DisplayName}#{InstanceId}";
}
