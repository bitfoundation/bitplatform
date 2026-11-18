using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.BevTools;

/// <summary>
/// Wraps each ComponentBase render fragment to measure BuildRenderTree, count renders, detect batches and attribute
/// causes. Runs on the renderer's dispatcher; the only shared state is the session's buffers.
/// </summary>
internal sealed class BevToolsRenderTracker
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> JsRuntimeProperties = new();
    private static readonly ConcurrentDictionary<(string Label, string Ancestor), string> ParentCauseDetails = new();
    private static readonly ConditionalWeakTable<Delegate, object> FragmentIds = new();
    private static readonly ConcurrentDictionary<Type, bool> StatefulFragmentTargets = new();
    private static long _nextFragmentId;
    private const string FragmentTokenPrefix = "RenderFragment #";
    private const string NewFragmentChange = "new RenderFragment instance (its output cannot be compared)";
    private const string StatefulFragmentChange = "RenderFragment reads component state when it runs (its output cannot be compared)";
    private const int MaxEntriesPerParameter = 150;
    private const string InitialCauseDetail = "First render";
    private const string DisabledCauseDetail = "Cause tracking disabled";
    private const string StateHasChangedCauseDetail = "StateHasChanged outside an event (async continuation, timer, state notification or explicit call)";
    private readonly BevToolsSession _session;
    private readonly BevToolsOptions _options;
    private readonly Queue<BevToolsRenderBatchInfo> _awaitingDiff = new();
    private readonly int _maxAwaitingDiff;
    private BevToolsRenderBatchInfo? _lastDiffBatch;
    private long _batchSequence;
    private long _batchStarts;
    private long _renderSequence;
    private long _lastRenderEndTicks;

    public BevToolsRenderTracker(BevToolsSession session, BevToolsOptions options)
    {
        _session = session;
        _options = options;
        _maxAwaitingDiff = Math.Clamp(options.MaxEvents, 1, 512);
    }

    public BevToolsRenderBatchInfo? CurrentBatch { get; private set; }

    public long BatchCount => Volatile.Read(ref _batchSequence);

    /// <summary>The <see cref="BevToolsRenderBatchInfo.Sequence"/> of the latest batch; the next one gets this plus one.</summary>
    internal long BatchStarts => Volatile.Read(ref _batchStarts);

    public bool DiffAttributionLost { get; private set; }

    public void Instrument(BevToolsComponentRecord record)
    {
        if (record.Component is not ComponentBase component)
        {
            record.InstrumentationNote = "Not derived from ComponentBase: renders are not measured, only presence in the tree.";
            return;
        }

        if (!BevToolsBlazorReflection.CanWrapRenderFragment)
        {
            record.InstrumentationNote = "ComponentBase._renderFragment not found on this framework version; render tracking unavailable.";
            return;
        }

        var original = BevToolsBlazorReflection.GetRenderFragment!(component);
        if (original is null)
        {
            record.InstrumentationNote = "Render fragment was null at activation.";
            return;
        }

        if (component is Microsoft.AspNetCore.Components.Forms.EditForm form && !record.IsBevTools)
        {
            // A form is watched from its first render, when its EditContext exists, so its first submit is seen even if
            // the panel was never opened - and again when the EditForm gets a new EditContext (a form reset after a
            // submit). Only an EditForm's fragment carries this; every other render is unchanged.
            var unwatched = original;
            var watched = new WeakReference<object?>(null);
            original = builder =>
            {
                unwatched(builder);
                var current = (object?)BevToolsFormTracker.ContextOfForm(form);
                if (current is not null && (!watched.TryGetTarget(out var last) || !ReferenceEquals(last, current)))
                {
                    watched.SetTarget(current);
                    _session.Forms.Watch(form, record);
                }
            };
        }

        RenderFragment wrapped = builder =>
        {
            var t0 = Stopwatch.GetTimestamp();
            var scope = BeginRender(record, component);
            // Read right around the fragment, after BevTools' own work for the render: only what BuildRenderTree allocates.
            var a1 = scope.Sample is not null && BevToolsAllocations.IsMeasurable ? GC.GetAllocatedBytesForCurrentThread() : -1;
            var t1 = Stopwatch.GetTimestamp();
            try
            {
                original(builder);
            }
            catch (Exception ex)
            {
                var t2 = Stopwatch.GetTimestamp();
                NoteAllocations(scope, a1);
                EndRender(record, scope, t1, t2, ex);
                if (record.IsBevTools)
                {
                    // A failed render of the panel's took the frame's time all the same.
                    NoteOwnRender(t1, t2);
                }

                _session.Overhead.AddInstrumentation((t1 - t0) + (Stopwatch.GetTimestamp() - t2));
                throw;
            }

            var t3 = Stopwatch.GetTimestamp();
            NoteAllocations(scope, a1);
            EndRender(record, scope, t1, t3, null);
            if (record.IsBevTools)
            {
                _session.Overhead.AddBevToolsRender(t3 - t1);
                NoteOwnRender(t1, t3);
            }
            else
            {
                _session.Overhead.AddInstrumentation((t1 - t0) + (Stopwatch.GetTimestamp() - t3));
            }
        };

        record.IsInstrumented = BevToolsBlazorReflection.TrySetRenderFragment(component, wrapped);
        if (!record.IsInstrumented)
        {
            record.InstrumentationNote = "Could not replace the render fragment.";
        }
    }

    private readonly record struct RenderScope(BevToolsRenderSample? Sample, BevToolsRenderBatchInfo? Batch);

    // When BevTools' own UI rendered, recently: a long frame on WebAssembly - where the panel renders on the page's only
    // thread - can be the panel's own work. A fixed array, written only for BevTools' renders, never allocating.
    private readonly (long Start, long End)[] _ownRenders = new (long, long)[256];
    private int _ownRenderNext;
    // Where the renders let go of to make room end: a window that starts before this has lost some of its own time.
    private long _ownRendersForgottenUntil;

    internal void NoteOwnRender(long start, long end)
    {
        lock (_ownRenders)
        {
            if (_ownRenders[_ownRenderNext].End > _ownRendersForgottenUntil)
            {
                _ownRendersForgottenUntil = _ownRenders[_ownRenderNext].End;
            }

            _ownRenders[_ownRenderNext] = (start, end);
            _ownRenderNext = (_ownRenderNext + 1) % _ownRenders.Length;
        }
    }

    /// <summary>
    /// How long BevTools' own components spent rendering between two timestamps, as far as the last 256 of their renders
    /// reach. <paramref name="complete"/> is false when renders inside the window were let go of: the time is then a lower
    /// bound - enough to say a frame was mostly BevTools', never enough to say it was not.
    /// </summary>
    internal double OwnRenderMsWithin(long from, long to, out bool complete)
    {
        long ticks = 0;
        lock (_ownRenders)
        {
            complete = _ownRendersForgottenUntil <= from;
            foreach (var (start, end) in _ownRenders)
            {
                if (end > from && start < to)
                {
                    ticks += Math.Min(end, to) - Math.Max(start, from);
                }
            }
        }

        return ticks * 1000.0 / Stopwatch.Frequency;
    }

    private static void NoteAllocations(RenderScope scope, long before)
    {
        if (before >= 0)
        {
            scope.Sample!.AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }

    private RenderScope BeginRender(BevToolsComponentRecord record, ComponentBase component)
    {
        if (!_session.IsEnabled)
        {
            return default;
        }

        if (!record.RendererResolved)
        {
            ResolveRenderer(record, component);
        }

        if (!record.JsRuntimePatched)
        {
            record.JsRuntimePatched = true;
            if (_options.TrackJsInterop && !record.IsBevTools)
            {
                PatchInjectedJsRuntime(record, component);
            }
        }

        if (record.IsBevTools)
        {
            // Not recorded, but the renderer still produces a batch and a diff measurement for it.
            if (DiffMatchingActive)
            {
                DetectBatch(record, track: false);
            }

            return default;
        }

        if (!_session.IsCapturing)
        {
            // Paused: the buffers must stay frozen, but the instance must still count as "has rendered" so the next
            // recorded render is not mislabeled as its first. This render is not compared, so the snapshots taken
            // before it no longer describe the previous render.
            _session.NoteSkippedRender(record);
            record.LastStateSnapshot = null;
            record.LastParameterSnapshot = null;
            if (DiffMatchingActive)
            {
                DetectBatch(record, track: false);
            }

            return default;
        }

        var batch = DetectBatch(record, track: true);
        var (cause, detail, triggerId) = DetermineCause(record, batch);
        var sample = new BevToolsRenderSample
        {
            Sequence = Interlocked.Increment(ref _renderSequence),
            At = DateTimeOffset.UtcNow,
            StartTicks = Stopwatch.GetTimestamp(),
            BatchId = batch?.Id ?? 0,
            Cause = cause,
            CauseDetail = detail,
            TriggerEventId = triggerId,
        };

        if (record.CaptureState || _options.CaptureComponentStateOnRender)
        {
            CaptureStateDiff(record, component, sample);
        }
        else if (record.LastStateSnapshot is not null || record.LastParameterSnapshot is not null)
        {
            // Not compared (the component is no longer inspected), so the snapshots stop describing the previous
            // render. Kept, they would make a parameter that changed and changed back look unchanged once the
            // component is inspected again, and call that render forced.
            record.LastStateSnapshot = null;
            record.LastParameterSnapshot = null;
        }

        return new RenderScope(sample, batch);
    }

    private void EndRender(BevToolsComponentRecord record, RenderScope scope, long startTicks, long endTicks, Exception? exception)
    {
        if (scope.Sample is null)
        {
            if (exception is not null && record.IsBevTools)
            {
                _session.Errors.Record(exception, "bevtools", component: record);
            }

            return;
        }

        var sample = scope.Sample;
        sample.DurationMs = Stopwatch.GetElapsedTime(startTicks, endTicks).TotalMilliseconds;
        sample.Failed = exception is not null;
        record.AddRender(sample);

        if (scope.Batch is { } batch)
        {
            batch.Count++;
            batch.TotalMs += sample.DurationMs;
            batch.RenderedInstanceIds.Add(record.InstanceId);
            batch.TriggerEventId ??= sample.TriggerEventId;
        }

        if (!_options.RecordRenderEvents)
        {
            if (exception is not null)
            {
                _session.Errors.Record(exception, "render", component: record, precedingEventId: sample.TriggerEventId);
            }

            _session.Touch();
            _lastRenderEndTicks = Stopwatch.GetTimestamp();
            return;
        }

        var title = sample.Cause == BevToolsRenderCause.Initial ? record.FirstRenderTitle : record.ReRenderTitle;
        var evt = new BevToolsEvent
        {
            Kind = BevToolsEventKind.Render,
            Category = "render",
            Title = title,
            Detail = sample.CauseDetail,
            DurationMs = sample.DurationMs,
            Severity = exception is null ? (sample.DurationMs >= _options.Diagnostics.SlowRenderMs ? BevToolsSeverity.Warning : BevToolsSeverity.Info) : BevToolsSeverity.Error,
            ParentEventId = sample.TriggerEventId ?? scope.Batch?.EventId,
            ComponentInstanceId = record.InstanceId,
            ComponentName = record.DisplayName,
            Timestamp = sample.At,
            StartTicks = sample.StartTicks,
            Data = new BevToolsRenderEventData(sample, record.RenderCount),
        };
        sample.EventId = _session.Timeline.Record(evt);

        if (exception is not null)
        {
            _session.Errors.Record(exception, "render", component: record, precedingEventId: sample.TriggerEventId);
        }

        _lastRenderEndTicks = Stopwatch.GetTimestamp();
    }

    private void ResolveRenderer(BevToolsComponentRecord record, ComponentBase component)
    {
        record.RendererResolved = true;
        if (!BevToolsBlazorReflection.CanResolveRenderer)
        {
            return;
        }

        var renderer = BevToolsBlazorReflection.ResolveRenderer(component, out var componentId);
        if (renderer is null)
        {
            return;
        }

        record.Renderer = renderer;
        record.ComponentId = componentId;
        record.RenderModeName = BevToolsRenderModes.Describe(BevToolsBlazorReflection.OwnRenderMode(renderer, component));
        _session.Components.NoteRenderer(renderer);

        // Rendering always happens on the renderer's synchronization context; remembering it makes later ambient
        // session lookups (HTTP, logs, metrics) an O(1) dictionary hit instead of a scan over every live session.
        BevToolsSessionResolver.NoteCurrentContext(_session);

        if (_session.Dispatcher is null)
        {
            try
            {
                _session.Dispatcher = renderer.Dispatcher;
            }
            catch
            {
                // some renderers throw for Dispatcher before initialization; ignore.
            }
        }

        if (_session.Platform is null)
        {
            var info = BevToolsBlazorReflection.ResolveRendererInfo(component);
            if (info is { } rendererInfo)
            {
                _session.Platform = rendererInfo.Name;
                _session.IsInteractive = rendererInfo.IsInteractive;
            }
        }

        _session.Components.ResolveHierarchy(record, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Whether <c>render_diff</c> measurements are expected, so closed batches wait in a queue for theirs. Never without
    /// the framework meters, unless a measurement was actually seen: batches would wait for a measurement that never
    /// comes until the queue overflowed.
    /// </summary>
    private bool DiffMatchingActive =>
        (_options.UseFrameworkInstrumentation || BevToolsActivityObserver.BatchMetricsObserved)
        && BevToolsCapabilityReport.MetricsAreSupported();

    /// <summary>
    /// Detects the batch a render belongs to. With <paramref name="track"/> false (BevTools' own renders, renders while
    /// paused) the batch is only bookkept: the renderer emits one diff measurement per batch whether BevTools records
    /// it or not, and a batch without an entry would hand its measurement to an application batch - the open one in
    /// WebAssembly, the next one in the queue on Server, shifting every later attribution by one. Bookkeeping needs
    /// the exact detector: the idle-time heuristic cannot tell where a batch of unrecorded renders ends.
    /// </summary>
    private BevToolsRenderBatchInfo? DetectBatch(BevToolsComponentRecord record, bool track)
    {
        var renderer = record.Renderer;
        if (!track && (renderer is null || !BevToolsBlazorReflection.CanDetectBatches))
        {
            return null;
        }

        var current = CurrentBatch;
        bool newBatch;
        if (renderer is not null && BevToolsBlazorReflection.CanDetectBatches)
        {
            try
            {
                var diffCount = BevToolsBlazorReflection.GetUpdatedDiffCount!(renderer);
                // The batch builder's diff list is cleared between batches, so a count of zero means the renderer just
                // started a new pass. A count lower than the renders we already saw means a new pass started with
                // renders we could not observe (components that do not derive from ComponentBase).
                newBatch = current is null || current.Closed || diffCount <= 0 || current.ObservedRenders > diffCount;
            }
            catch
            {
                // A runtime reflection failure must degrade to the documented heuristic. Treating it as a zero diff
                // count splits every component render into a separate batch and produces confidently wrong data.
                newBatch = current is null || current.Closed || Stopwatch.GetElapsedTime(_lastRenderEndTicks).TotalMilliseconds > 2;
            }
        }
        else
        {
            // Fallback heuristic: renders separated by more than 2 ms of idle time belong to different batches.
            newBatch = current is null || current.Closed || Stopwatch.GetElapsedTime(_lastRenderEndTicks).TotalMilliseconds > 2;
        }

        if (!newBatch)
        {
            current!.ObservedRenders++;
            if (track && !current.IsTracked)
            {
                // The first application render in a batch that so far held only unrecorded renders.
                StartTracking(current, record);
            }

            return current;
        }

        CloseBatch();
        var batch = new BevToolsRenderBatchInfo { StartTicks = Stopwatch.GetTimestamp(), ObservedRenders = 1, Sequence = Interlocked.Increment(ref _batchStarts) };
        if (track)
        {
            StartTracking(batch, record);
        }

        CurrentBatch = batch;
        return batch;
    }

    private void StartTracking(BevToolsRenderBatchInfo batch, BevToolsComponentRecord firstRender)
    {
        batch.IsTracked = true;
        batch.Id = Interlocked.Increment(ref _batchSequence);
        batch.EventId = _session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.RenderBatch,
            Category = "render-batch",
            Title = "Render batch #" + batch.Id,
            Detail = "in progress",
            ParentEventId = BevToolsActivityObserver.CurrentTriggerEvent(_session, firstRender, batch)?.Id,
            StartTicks = batch.StartTicks,
            Data = new Dictionary<string, object?> { ["batch"] = batch.Id },
        });
    }

    /// <summary>Finalizes the current batch event. Called when the next batch starts and by the UI refresh.</summary>
    public void CloseBatch()
    {
        if (CurrentBatch is not { Closed: false } batch)
        {
            return;
        }

        batch.Closed = true;
        batch.RenderedInstanceIds.Clear();
        if (batch.DiffMs is null && DiffMatchingActive)
        {
            if (DiffAttributionLost)
            {
                UpdateBatchEvent(batch);
                return;
            }

            _awaitingDiff.Enqueue(batch);
            if (_awaitingDiff.Count > _maxAwaitingDiff)
            {
                _awaitingDiff.Clear();
                _lastDiffBatch = null;
                DiffAttributionLost = true;
                _session.Timeline.Record(new BevToolsEvent
                {
                    Kind = BevToolsEventKind.Diagnostic,
                    Category = "bevtools",
                    Title = "Render-diff attribution disabled",
                    Detail = $"More than {_maxAwaitingDiff} render batches were awaiting browser acknowledgement. Later diff measurements are omitted rather than attached to the wrong batch.",
                    Severity = BevToolsSeverity.Warning,
                });
            }
        }

        UpdateBatchEvent(batch);
    }

    private void UpdateBatchEvent(BevToolsRenderBatchInfo batch)
    {
        if (!batch.IsTracked)
        {
            return;
        }

        _session.Timeline.Update(batch.EventId, evt =>
        {
            evt.DurationMs = batch.TotalMs;
            evt.Detail = $"{batch.Count} component{(batch.Count == 1 ? "" : "s")} rendered, {batch.TotalMs:0.00} ms building"
                + (batch.DiffMs is { } d ? $", {d:0.00} ms diffing" : "")
                + (batch.DiffSize is { } s ? $", {s} DOM edits" : "");
            if (batch.Count >= _options.Diagnostics.RenderCascadeSize)
            {
                evt.Severity = BevToolsSeverity.Warning;
            }

            if (evt.Data is Dictionary<string, object?> data)
            {
                data["count"] = batch.Count;
                data["totalMs"] = batch.TotalMs;
                data["diffMs"] = batch.DiffMs;
                data["diffSize"] = batch.DiffSize;
            }
        });
    }

    /// <summary>
    /// Called from the <c>aspnetcore.components.render_diff.duration</c> measurement, once per batch.
    /// <para>
    /// The framework records this metric when the batch <em>task</em> completes. In WebAssembly that is synchronous,
    /// so the measurement belongs to the batch still open. On Blazor Server the task completes when the browser
    /// acknowledges the render, which can be after later batches have started — so measurements are matched against
    /// the queue of batches still waiting for one, in the order the renderer produced them.
    /// </para>
    /// </summary>
    internal void RecordBatchDiff(double diffMs)
    {
        if (DiffAttributionLost)
        {
            return;
        }

        // A batch that already has its measurement is never given a second one: that would overwrite a closed batch's
        // numbers with those of a batch BevTools could not see (one made only of unobserved components).
        var batch = TakeBatchAwaitingDiff() ?? (CurrentBatch is { DiffMs: null } current ? current : null);
        if (batch is null)
        {
            return;
        }

        _lastDiffBatch = batch;
        batch.DiffMs = diffMs;
        if (ReferenceEquals(batch, CurrentBatch))
        {
            CloseBatch();
        }
        else
        {
            UpdateBatchEvent(batch);
        }
    }

    /// <summary>Called from the <c>aspnetcore.components.render_diff.size</c> measurement, which follows the duration.</summary>
    internal void RecordBatchDiffSize(int diffSize)
    {
        if (DiffAttributionLost)
        {
            return;
        }

        var batch = _lastDiffBatch ?? CurrentBatch;
        if (batch is null)
        {
            return;
        }

        batch.DiffSize = diffSize;
        UpdateBatchEvent(batch);
    }

    private BevToolsRenderBatchInfo? TakeBatchAwaitingDiff()
    {
        while (_awaitingDiff.TryDequeue(out var batch))
        {
            // Server diff metrics are emitted only after the browser acknowledges the batch. Slow or disconnected
            // clients can legitimately take far longer than a few seconds, so preserve FIFO attribution until the
            // configured bounded history evicts the bookkeeping entry.
            if (batch.DiffMs is null)
            {
                return batch;
            }
        }

        return null;
    }

    private (BevToolsRenderCause Cause, string Detail, long? TriggerEventId) DetermineCause(BevToolsComponentRecord record, BevToolsRenderBatchInfo? batch)
    {
        if (!record.HasRendered)
        {
            return (BevToolsRenderCause.Initial, InitialCauseDetail, BevToolsActivityObserver.CurrentTriggerEvent(_session, record, batch)?.Id);
        }

        if (!_options.TrackRenderCauses)
        {
            return (BevToolsRenderCause.Unknown, DisabledCauseDetail, null);
        }

        // 1. An ancestor rendered earlier in the same batch: the renderer re-set our parameters. It is the parent when no
        // component the tree shows sits between the two: framework plumbing such as a CascadingValue is hidden there,
        // and the component whose markup created this one is what the tree, and a reader, call its parent.
        if (batch is not null && batch.Count > 0)
        {
            var ancestor = record.Parent;
            var hops = 0;
            var visibleBetween = 0;
            while (ancestor is not null && hops++ < 64)
            {
                if (batch.RenderedInstanceIds.Contains(ancestor.InstanceId))
                {
                    var ancestorSample = ancestor.LastRender;
                    var triggerId = ancestorSample?.TriggerEventId;
                    var label = visibleBetween == 0 ? "Parent" : "Ancestor";
                    var detail = ParentCauseDetails.GetOrAdd((label, ancestor.DisplayName), static key => $"{key.Label} {key.Ancestor} re-rendered (parameters re-applied)");
                    return (BevToolsRenderCause.ParentRender, detail, triggerId ?? ancestorSample?.EventId);
                }

                if (!ancestor.IsHidden)
                {
                    visibleBetween++;
                }

                ancestor = ancestor.Parent;
            }
        }

        // 2. A UI event or navigation is executing on this session, or this batch renders what one had queued.
        var trigger = BevToolsActivityObserver.CurrentTriggerEvent(_session, record, batch);
        if (trigger is not null)
        {
            return trigger.Kind == BevToolsEventKind.Navigation
                ? (BevToolsRenderCause.Navigation, "Navigation: " + trigger.Title, trigger.Id)
                : (BevToolsRenderCause.UiEvent, "Event: " + trigger.Title, trigger.Id);
        }

        // 3. Anything else is an explicit StateHasChanged / async continuation (timer, HTTP completion, state notification).
        return (BevToolsRenderCause.StateHasChanged, StateHasChangedCauseDetail, null);
    }

    /// <summary>
    /// Snapshots the component's fields and parameters before the render and diffs them against the previous
    /// captured render. Fields answer "what state changed?"; parameters answer "did the parent give me anything new?",
    /// which is what separates a needed re-render from one the parent merely forced.
    /// <para>
    /// Runs for the inspected component, and for every component on every render when
    /// <see cref="BevToolsOptions.CaptureComponentStateOnRender"/> is on, so the parameter walk shares one wall-clock
    /// budget per render rather than one per parameter.
    /// </para>
    /// </summary>
    private void CaptureStateDiff(BevToolsComponentRecord record, ComponentBase component, BevToolsRenderSample sample)
    {
        // Under ValueAccess None every value displays as hidden, so nothing could be told apart; under Shallow an object
        // shows only its type, so a changed list item would look unchanged and the render would be called forced.
        // Neither compares parameters, which leaves the render "not compared" rather than wrongly "forced".
        var access = _session.ValueAccess;
        if (access == BevToolsValueAccess.None)
        {
            return;
        }

        var start = Stopwatch.GetTimestamp();
        try
        {
            var snapshot = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (name, value, member) in BevToolsComponentStateReader.ReadFields(component))
            {
                // The member is passed so [BevToolsSensitive] is honoured, not only the name patterns.
                snapshot[name] = _session.Inspector.Describe(name, value, "", member).Display;
            }

            if (record.LastStateSnapshot is not null)
            {
                var changes = BevToolsObjectInspector.Diff(record.LastStateSnapshot, snapshot);
                if (changes.Count > 0)
                {
                    sample.ChangedState = FormatChanges(changes);
                }
            }

            record.LastStateSnapshot = snapshot;
            if (access != BevToolsValueAccess.Full)
            {
                return;
            }

            var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<string, string>? fragmentChanges = null;
            var deadline = BevToolsObjectInspector.FlattenDeadline();
            foreach (var (name, value, property) in BevToolsComponentStateReader.ReadParameters(component))
            {
                if (value is Delegate fragment && IsRenderFragment(fragment.GetType()))
                {
                    // Every fragment displays as "RenderFragment", so comparing displays would call new child content
                    // identical. Compare the instance instead, and never call a fragment unchanged when it reads
                    // component state as it runs: the same instance can then render different output.
                    var instance = FragmentTokenPrefix + FragmentInstanceId(fragment);
                    var readsState = RenderFragmentReadsState(fragment);
                    parameters[name] = readsState ? instance + " @" + sample.Sequence : instance;
                    var sameInstance = record.LastParameterSnapshot?.TryGetValue(name, out var previous) == true
                        && previous.StartsWith(instance, StringComparison.Ordinal)
                        && (previous.Length == instance.Length || previous[instance.Length] == ' ');
                    (fragmentChanges ??= new(StringComparer.Ordinal))[name] = sameInstance ? StatefulFragmentChange : NewFragmentChange;
                    continue;
                }

                // The parameter's display under its name, then two levels of members (≤ 25 collection items) below it,
                // so a list whose item changed is not mistaken for an identical parameter. The inspector redacts
                // sensitive parameters and does not walk them.
                _session.Inspector.FlattenNamed(parameters, name, value, property, maxDepth: 2, maxItems: 25, maxEntries: parameters.Count + MaxEntriesPerParameter, deadline);
            }

            if (record.LastParameterSnapshot is not null)
            {
                var changes = BevToolsObjectInspector.Diff(record.LastParameterSnapshot, parameters);
                // A non-null list is what marks the sample as compared; the empty case shares one array.
                sample.ChangedParameters = changes.Count == 0 ? Array.Empty<string>() : FormatChanges(changes, fragmentChanges);
            }

            record.LastParameterSnapshot = parameters;
        }
        catch
        {
            // state capture is best-effort
        }
        finally
        {
            _session.Overhead.AddInspection(Stopwatch.GetTimestamp() - start);
        }
    }

    private static string[] FormatChanges(List<BevToolsStateDiffEntry> changes, Dictionary<string, string>? fragmentChanges = null) =>
        changes.Select(c => c.After is not null && fragmentChanges is not null && fragmentChanges.TryGetValue(c.Path, out var fragmentChange)
            ? c.Path + ": " + fragmentChange
            : c.Path + ": " + FormatValue(c.Before) + " → " + FormatValue(c.After)).ToArray();

    /// <summary>Shows a fragment's comparison token as the type name: the instance number means nothing to a reader.</summary>
    private static string FormatValue(string? value) =>
        value is null ? "∅" : value.StartsWith(FragmentTokenPrefix, StringComparison.Ordinal) ? "RenderFragment" : value;

    private static bool IsRenderFragment(Type type) =>
        type == typeof(RenderFragment) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>));

    /// <summary>A number unique to one delegate instance for as long as it lives. Not an identity hash code, which can repeat.</summary>
    private static object FragmentInstanceId(Delegate fragment) =>
        FragmentIds.GetValue(fragment, static _ => Interlocked.Increment(ref _nextFragmentId));

    /// <summary>
    /// True when the fragment can render different output without being recreated. That is the case when its target
    /// is a component (a method group, or a lambda the component cached in a field, reads the component's fields as it
    /// runs), a compiler-generated closure that holds a component, or any other object BevTools cannot see into.
    /// Non-capturing child content compiles to one cached delegate on the compiler's empty closure class; that one
    /// renders the same output every time and is compared by instance.
    /// </summary>
    private static bool RenderFragmentReadsState(Delegate fragment) => fragment.Target is { } target
        && (target is IComponent || StatefulFragmentTargets.GetOrAdd(target.GetType(), static type =>
            !type.IsDefined(typeof(CompilerGeneratedAttribute), false)
            || type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(f => typeof(IComponent).IsAssignableFrom(f.FieldType))));

    /// <summary>
    /// Replaces the component's <c>[Inject] IJSRuntime</c> with a tracking decorator. This happens on the first render,
    /// which is the earliest point after the framework has performed property injection; calls made from
    /// <c>OnInitialized(Async)</c> before that first render are therefore not attributed to the component.
    /// </summary>
    private void PatchInjectedJsRuntime(BevToolsComponentRecord record, ComponentBase component)
    {
        var properties = JsRuntimeProperties.GetOrAdd(component.GetType(), static type =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(p => p.IsDefined(typeof(InjectAttribute), true) && p.CanWrite && p.CanRead
                    && (p.PropertyType == typeof(IJSRuntime) || p.PropertyType == typeof(IJSInProcessRuntime)))
                .ToArray());
        foreach (var property in properties)
        {
            try
            {
                var current = property.GetValue(component);
                if (current is null or BevToolsTrackingJSRuntime || BevToolsBlazorReflection.IsJSRuntimeNotReady(current))
                {
                    continue;
                }

                if (current is IJSRuntime js)
                {
                    var wrapper = BevToolsTrackingJSRuntime.Create(js, _session, record);
                    if (property.PropertyType.IsInstanceOfType(wrapper))
                    {
                        property.SetValue(component, wrapper);
                    }
                }
            }
            catch
            {
                // never let instrumentation break the app
            }
        }
    }
}
