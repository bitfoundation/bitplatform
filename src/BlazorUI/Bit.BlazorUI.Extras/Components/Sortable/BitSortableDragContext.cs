namespace Bit.BlazorUI;

/// <summary>
/// Tracks the in-flight drag across all participating zones. A single instance lives on
/// <see cref="BitSortableCoordinator"/> for the duration of one drag.
/// </summary>
internal sealed class BitSortableDragContext
{
    /// <summary>
    /// Names this drag to the browser layer, which sends it back with every call about the drag: a call that arrives
    /// late, about a drag that has since ended, is never taken for one about the drag in progress.
    /// </summary>
    public int Id { get; init; }

    /// <summary>The data item currently being dragged (the clone once a clone has been dropped).</summary>
    public required object Item { get; set; }

    /// <summary>The original item where the drag started (equals <see cref="Item"/> unless cloned).</summary>
    public required object OriginalItem { get; init; }

    /// <summary>The zone where the drag began.</summary>
    public required IBitSortableZone Source { get; init; }

    /// <summary>The zone that currently contains <see cref="Item"/>.</summary>
    public required IBitSortableZone CurrentZone { get; set; }

    /// <summary>Index of the item in the source list when the drag began.</summary>
    public int OldIndex { get; init; }

    /// <summary>The effective pull mode resolved from the source list.</summary>
    public BitSortablePull PullMode { get; init; }

    /// <summary>How the drag is made.</summary>
    public BitSortableInput Input { get; init; }

    /// <summary>The zone currently showing the drop placeholder (a target preview), if any.</summary>
    public IBitSortableZone? PlaceholderZone { get; set; }

    /// <summary>Index within <see cref="PlaceholderZone"/> where the item would be inserted.</summary>
    public int PlaceholderIndex { get; set; }

    /// <summary>
    /// In swap mode, the zone that holds the item currently highlighted as the swap target (the item the
    /// dragged one will trade places with on drop), if any.
    /// </summary>
    public IBitSortableZone? SwapTargetZone { get; set; }

    /// <summary>In swap mode, the item highlighted as the swap target, if any.</summary>
    public object? SwapTargetItem { get; set; }

    /// <summary>The item the drop traded the dragged item for, once a swap is committed; <c>null</c> otherwise.</summary>
    public object? SwappedWith { get; set; }

    /// <summary>
    /// In multi-drag mode, the full ordered set of items being dragged together (including the primary
    /// <see cref="Item"/>), in source-list order. <c>null</c> for a single-item drag.
    /// </summary>
    public List<object>? MultiItems { get; set; }

    /// <summary>
    /// Every zone whose bound list this drag has modified, with its items as they were just before the drag
    /// first modified it. Comparing against that snapshot, rather than remembering that a list was touched,
    /// is what tells a real change from a round trip (an item dragged away and back), and taking it at the
    /// first modification rather than at dragstart keeps an edit the app made to its own list in the
    /// meantime from being reported as the drag's.
    /// </summary>
    private readonly Dictionary<IBitSortableZone, object?[]> _before = new(ReferenceEqualityComparer.Instance);

    /// <summary>The zones this drag has modified (see <see cref="Touch"/>).</summary>
    public IEnumerable<IBitSortableZone> TouchedZones => _before.Keys;

    /// <summary>Records the items of <paramref name="zone"/>; call it right before the drag modifies that zone.</summary>
    public void Touch(IBitSortableZone zone)
    {
        if (_before.ContainsKey(zone)) return;

        var items = new object?[zone.Count];
        for (int i = 0; i < items.Length; i++)
            items[i] = zone.ItemAt(i);
        _before[zone] = items;
    }

    /// <summary>Whether this drag left <paramref name="zone"/> in a different order than it found it.</summary>
    public bool Changed(IBitSortableZone zone)
    {
        if (!_before.TryGetValue(zone, out var before)) return false;
        if (before.Length != zone.Count) return true;
        for (int i = 0; i < before.Length; i++)
            if (!Equals(before[i], zone.ItemAt(i))) return true;
        return false;
    }

    /// <summary>Puts <paramref name="zone"/> back to the items it held before this drag first changed it.</summary>
    public void Restore(IBitSortableZone zone)
    {
        if (_before.TryGetValue(zone, out var before)) zone.SetItems(before);
    }

    /// <summary>Guards against finalizing the same drag twice (drop + dragend).</summary>
    public bool Finished { get; set; }

    // What the app's code threw while this drag was decided or reported, in the order it was thrown (see Fail).
    private List<Exception>? _failures;

    /// <summary>
    /// Keeps <paramref name="exception"/>, which the app's code threw during this drag - <c>CanDrop</c> inside the gate
    /// and <c>ConfirmDrop</c>, taken as a refusal there, or a handler of the events that report it - for the call that
    /// met it to pass on once it is done (see <see cref="TakeFailure"/>). The drag stays whole and is still reported:
    /// one handler that throws keeps no other from hearing of it. Every exception is kept, after those before it, so
    /// the one that refused the drag comes first.
    /// </summary>
    public void Fail(Exception exception) => (_failures ??= []).Add(exception);

    /// <summary>
    /// What the drag kept to pass on (see <see cref="Fail"/>), which it no longer holds - each is passed on once: the
    /// one exception as it was thrown, several together in an <see cref="AggregateException"/>, in the order they were
    /// thrown; <c>null</c> when there is none.
    /// </summary>
    public Exception? TakeFailure()
    {
        var failures = _failures;
        _failures = null;
        return failures switch
        {
            null => null,
            [var failure] => failure,
            _ => new AggregateException(failures)
        };
    }
}
