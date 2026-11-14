using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>A reference BevTools found to a disposed component that is still alive: something that keeps it.</summary>
/// <param name="Holder">What holds it: "FilterState.Changed (injected)", "the JS runtime", "its own field _timer".</param>
/// <param name="Detail">How: "handler ProductRow.OnFilterChanged", "lambda in ProductRow.OnInitialized captures it".</param>
/// <param name="Suggestion">What releases it.</param>
public sealed record BevRetentionCause(string Holder, string Detail, string Suggestion);

/// <summary>
/// Disposed components of one type that garbage collection did not reclaim: the renderer let go of them, and something
/// else still holds them, and through them whatever they reference. Each is the topmost of a retained subtree: the
/// disposed components it rendered that are still alive too are counted in <see cref="DescendantCount"/>, not reported
/// on their own - a page kept alive keeps every row and icon it rendered, and that is one finding, not forty.
/// </summary>
/// <param name="TypeName">The component type, namespace included.</param>
/// <param name="DisplayName">The type without its namespace.</param>
/// <param name="Count">How many disposed instances of it are alive.</param>
/// <param name="OldestDisposal">When the longest-retained of them was disposed.</param>
/// <param name="FullCollections">Full (generation 2) collections every one of them survived since it was free to go (past its grace period, out of the renderer's buffers).</param>
/// <param name="Causes">
/// References found to the instances that were analysed, and to the retained components they rendered (a cause found on
/// one of those says so); empty when none of the places BevTools looks holds them.
/// </param>
/// <param name="InstanceIds">BevTools' instance ids of the retained instances, then of their retained descendants (at most 50 in all), as the timeline and errors name them.</param>
public sealed record BevRetainedComponents(
    string TypeName,
    string DisplayName,
    int Count,
    DateTimeOffset OldestDisposal,
    int FullCollections,
    IReadOnlyList<BevRetentionCause> Causes,
    IReadOnlyList<long> InstanceIds)
{
    /// <summary>The disposed components these rendered (by the parent BevTools recorded for each) that are still alive with them.</summary>
    public int DescendantCount { get; init; }

    /// <summary><see cref="DescendantCount"/> by type, the most numerous first.</summary>
    public IReadOnlyList<BevRetainedTypeCount> Descendants { get; init; } = [];
}

/// <summary>How many retained components of one type a retained subtree holds.</summary>
public sealed record BevRetainedTypeCount(string DisplayName, int Count);

/// <summary>
/// Remembers disposed components weakly and tells which ones survive garbage collection. An instance is first free to
/// go once its grace period is over - work still running when the component was removed (an awaited request)
/// legitimately holds it for a moment - and the renderer no longer has it in a buffer (its parent's previous render
/// tree keeps it until the parent renders again). Only full collections after that moment count: "still alive" is
/// claimed after two of them, before which it is simply not collected yet. Nothing here keeps a component alive.
/// </summary>
internal sealed class BevLeakTracker(BevToolsSession session)
{
    internal const int MaxProbes = 2000;
    private const int MaxAnalysedPerType = 5;
    private const int MaxAnalysedDescendants = 20;
    private const int MaxAncestors = 64;
    private const int RequiredFullCollections = 2;

    private readonly object _lock = new();
    private readonly LinkedList<Probe> _probes = new();

    /// <summary>How long after its disposal a component may legitimately still be held (work it started still running).</summary>
    internal TimeSpan GracePeriod { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>The same, when the developer forced the collection: they asked about what they just did.</summary>
    internal TimeSpan ForcedGracePeriod { get; set; } = TimeSpan.FromSeconds(1);

    private sealed class Probe(long instanceId, long[] ancestors, Type type, string displayName, DateTimeOffset disposedAt, int fullCollections, IComponent component)
    {
        private readonly WeakReference<IComponent> _component = new(component);

        public long InstanceId { get; } = instanceId;

        /// <summary>
        /// The components above it while it was on the page, its parent first. The whole chain, not the parent alone: a
        /// <c>CascadingValue</c> between a retained component and the one that rendered it is usually collected.
        /// </summary>
        public long[] Ancestors { get; } = ancestors;

        public Type Type { get; } = type;

        public string DisplayName { get; } = displayName;

        public DateTimeOffset DisposedAt { get; } = disposedAt;

        public int FullCollectionsAtDisposal { get; } = fullCollections;

        /// <summary>Full collections when it was first seen free to go; null until then, and again while the renderer holds it.</summary>
        public int? EligibleAtCollections { get; set; }

        /// <summary>Whether the renderer's buffers held it when they were last read, and when that was.</summary>
        public bool HeldByRenderer { get; set; }

        public DateTimeOffset? HeldCheckedAt { get; set; }

        /// <summary>
        /// A forced check found a singleton or scoped holder still referencing it after its disposal: retained whatever
        /// the renderer's buffers still hold, until it is collected.
        /// </summary>
        public bool ConfirmedByCause { get; set; }

        public IReadOnlyList<BevRetentionCause>? Causes { get; set; }

        public IComponent? Component => _component.TryGetTarget(out var c) ? c : null;
    }

    /// <summary>When the developer last asked for a collection and check; null when BevTools never forced one.</summary>
    public DateTimeOffset? LastForcedCheckAt { get; private set; }

    /// <summary>Disposed components remembered and not collected yet, retained or not.</summary>
    public int Watched
    {
        get { lock (_lock) { return _probes.Count; } }
    }

    /// <summary>Remembers a component the renderer just disposed. Bounded: the oldest disposals are forgotten first.</summary>
    public void Track(BevComponentRecord record, IComponent component)
    {
        if (record.IsBevTools)
        {
            return;
        }

        var ancestors = new List<long>();
        for (var parent = record.Parent; parent is not null && ancestors.Count < MaxAncestors; parent = parent.Parent)
        {
            ancestors.Add(parent.InstanceId);
        }

        var probe = new Probe(record.InstanceId, [.. ancestors], record.Type, record.DisplayName, record.DisposedAt ?? DateTimeOffset.UtcNow, FullCollections(), component);
        lock (_lock)
        {
            _probes.AddLast(probe);
            while (_probes.Count > MaxProbes)
            {
                _probes.RemoveFirst();
            }
        }
    }

    /// <summary>
    /// The disposed components that are still alive after full collections since their disposal, by the type of the
    /// topmost retained component of each subtree, with the references found to them. Collected ones are forgotten as a
    /// side effect.
    /// </summary>
    public BevRetainedComponents[] Retained()
    {
        var full = FullCollections();
        var live = Live();
        MarkEligible(live, GracePeriod, full, fresh: false);
        var retained = live.Where(l => l.Probe.ConfirmedByCause || (l.Probe.EligibleAtCollections is { } at && full - at >= RequiredFullCollections)).ToList();

        // A retained component with a retained ancestor goes under the topmost one: whatever keeps a page keeps what it
        // rendered (through its fields and cascading values), and the children's render fragments keep the page. The
        // subtree is reported once, at its top.
        var byId = retained.ToDictionary(r => r.Probe.InstanceId);
        var subtrees = retained.GroupBy(r => TopOf(r.Probe, byId).InstanceId).Select(g => (Top: byId[g.Key], Members: g.ToList())).ToList();

        // What holds them, for a few of each type and a few of what they rendered: one scan reads each shared holder
        // once, however many rows leaked.
        var scan = new BevRetentionAnalyzer.Scan(session, live.Select(l => l.Component));
        var result = new List<BevRetainedComponents>();
        foreach (var group in subtrees.GroupBy(s => s.Top.Probe.Type))
        {
            var tops = group.Select(s => s.Top).ToList();
            var descendants = group.SelectMany(s => s.Members.Where(m => !ReferenceEquals(m.Probe, s.Top.Probe))).ToList();
            var causes = new List<BevRetentionCause>();
            foreach (var (probe, component) in tops.Take(MaxAnalysedPerType))
            {
                causes.AddRange(probe.Causes ??= BevRetentionAnalyzer.Analyze(scan, component));
            }

            // One of each type first: a hundred icons share one cause, if any.
            foreach (var (probe, component) in descendants.GroupBy(d => d.Probe.Type).Select(g => g.First()).Take(MaxAnalysedDescendants))
            {
                probe.Causes ??= BevRetentionAnalyzer.Analyze(scan, component);
                causes.AddRange(probe.Causes.Where(c => c.Holder != "BevTools").Select(c => c with { Detail = c.Detail + ", on " + probe.DisplayName + ", one of the components it rendered" }));
            }

            var first = tops[0].Probe;
            var members = group.SelectMany(s => s.Members).ToList();
            result.Add(new BevRetainedComponents(
                first.Type.FullName ?? first.Type.Name,
                first.DisplayName,
                tops.Count,
                members.Min(r => r.Probe.DisposedAt),
                full - members.Max(r => r.Probe.EligibleAtCollections ?? full),
                causes.Distinct().Take(10).ToArray(),
                tops.Concat(descendants).Select(r => r.Probe.InstanceId).Take(50).ToArray())
            {
                DescendantCount = descendants.Count,
                Descendants = descendants.GroupBy(d => d.Probe.DisplayName).Select(g => new BevRetainedTypeCount(g.Key, g.Count()))
                    .OrderByDescending(c => c.Count).ThenBy(c => c.DisplayName, StringComparer.Ordinal).ToArray(),
            });
        }

        return result.OrderByDescending(r => r.Count + r.DescendantCount).ToArray();
    }

    /// <summary>
    /// The topmost retained ancestor of a retained component, by the ancestors recorded while it was on the page; itself
    /// when none of them is retained.
    /// </summary>
    private static Probe TopOf(Probe probe, Dictionary<long, (Probe Probe, IComponent Component)> retained)
    {
        for (var i = probe.Ancestors.Length - 1; i >= 0; i--)
        {
            if (retained.TryGetValue(probe.Ancestors[i], out var above))
            {
                return above.Probe;
            }
        }

        return probe;
    }

    /// <summary>The probes whose component is still alive; the others are forgotten.</summary>
    private List<(Probe Probe, IComponent Component)> Live()
    {
        var live = new List<(Probe, IComponent)>();
        lock (_lock)
        {
            for (var node = _probes.First; node is not null;)
            {
                var next = node.Next;
                if (node.Value.Component is { } component)
                {
                    live.Add((node.Value, component));
                }
                else
                {
                    _probes.Remove(node);
                }

                node = next;
            }
        }

        return live;
    }

    /// <summary>
    /// Starts the count of full collections for each instance past <paramref name="grace"/> that the renderer no
    /// longer holds, and stops it again for one the renderer holds: until the parent renders again, its previous render
    /// tree keeps the instance, which is not a leak and cannot be judged yet. The renderer's buffers are read at most
    /// once a second per instance (the Runtime panel asks on every render), and only a yes or no is kept from them:
    /// keeping their components between calls would itself keep them alive.
    /// </summary>
    private void MarkEligible(List<(Probe Probe, IComponent Component)> live, TimeSpan grace, int full, bool fresh)
    {
        var now = DateTimeOffset.UtcNow;
        var due = live.Where(l => (now - l.Probe.DisposedAt >= grace || l.Probe.EligibleAtCollections is not null)
            && (fresh || l.Probe.HeldCheckedAt is not { } checkedAt || now - checkedAt >= TimeSpan.FromSeconds(1))).ToList();
        if (due.Count > 0)
        {
            var held = BevRetentionAnalyzer.HeldByRenderer(session);
            foreach (var (probe, component) in due)
            {
                probe.HeldByRenderer = held.Contains(component);
                probe.HeldCheckedAt = now;
            }
        }

        foreach (var (probe, _) in live)
        {
            if (probe.HeldCheckedAt is null || (now - probe.DisposedAt < grace && probe.EligibleAtCollections is null))
            {
                continue;
            }

            if (probe.HeldByRenderer && !probe.ConfirmedByCause)
            {
                probe.EligibleAtCollections = null;
                probe.Causes = null;
            }
            else
            {
                probe.EligibleAtCollections ??= full;
            }
        }
    }

    /// <summary>
    /// After forced collections: an instance the renderer still holds cannot be judged by survival, but a singleton or
    /// scoped holder that references it after its disposal is evidence on its own - the renderer lets go with the
    /// parent's next render, the holder never does. Such an instance is retained from now on, with that cause.
    /// </summary>
    private void ConfirmHeldByCause(int collectionsBefore)
    {
        var now = DateTimeOffset.UtcNow;
        var live = Live();
        var scan = new BevRetentionAnalyzer.Scan(session, live.Select(l => l.Component));
        foreach (var (probe, component) in live)
        {
            if (probe.ConfirmedByCause || !probe.HeldByRenderer || now - probe.DisposedAt < ForcedGracePeriod)
            {
                continue;
            }

            var causes = BevRetentionAnalyzer.Analyze(scan, component);
            if (causes.Count > 0 && causes.All(c => c.Holder != "BevTools"))
            {
                probe.Causes = causes;
                probe.ConfirmedByCause = true;
                probe.EligibleAtCollections = collectionsBefore;
            }
        }
    }

    /// <summary>
    /// Runs full blocking collections now, then answers <see cref="Retained"/>: what a developer asks for to know at
    /// once instead of waiting for the runtime's own full collections. The caller decides whether that is acceptable.
    /// </summary>
    public BevRetainedComponents[] CollectAndCheck()
    {
        // Disposal is noticed when the registry refreshes; what is gone already must be watched before collecting.
        session.Components.Refresh(force: true);

        // What the developer just removed is free to go after the shorter grace, and the collections below are the
        // ones that count for it. It is marked after them, with the count from before: a collection does not change what
        // the renderer's buffers hold, and nothing BevTools touched is on the stack while they run (debug builds keep
        // locals alive to the end of a method, and WebAssembly scans its stack conservatively, stale slots included).
        var before = FullCollections();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        MarkEligible(Live(), ForcedGracePeriod, before, fresh: true);
        ConfirmHeldByCause(before);
        LastForcedCheckAt = DateTimeOffset.UtcNow;
        return Retained();
    }

    /// <summary>
    /// <see cref="CollectAndCheck"/> for the panel: a component whose disposal was only just noticed gets its grace
    /// period first, so what the developer removed a moment ago is checked too instead of silently skipped.
    /// </summary>
    public async Task<BevRetainedComponents[]> CollectAndCheckAsync()
    {
        session.Components.Refresh(force: true);
        DateTimeOffset? newest;
        lock (_lock)
        {
            newest = _probes.Count == 0 ? null : _probes.Max(p => p.DisposedAt);
        }

        if (newest is { } at && ForcedGracePeriod - (DateTimeOffset.UtcNow - at) is { Ticks: > 0 } wait)
        {
            await Task.Delay(wait);
        }

        return CollectAndCheck();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _probes.Clear();
        }
    }

    private static int FullCollections() => GC.CollectionCount(GC.MaxGeneration);
}
