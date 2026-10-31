namespace Bit.Bortable;

/// <summary>
/// Tracks the in-flight drag across all participating zones. A single instance lives on
/// <see cref="BortableService"/> for the duration of one drag.
/// </summary>
internal sealed class BortableDragContext
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
    public required IBortableZone Source { get; init; }

    /// <summary>The zone that currently contains <see cref="Item"/>.</summary>
    public required IBortableZone CurrentZone { get; set; }

    /// <summary>Index of the item in the source list when the drag began.</summary>
    public int OldIndex { get; init; }

    /// <summary>The effective pull mode resolved from the source list.</summary>
    public BortablePull PullMode { get; init; }

    /// <summary>How the drag is made.</summary>
    public BortableInput Input { get; init; }

    /// <summary>The zone currently showing the drop placeholder (a target preview), if any.</summary>
    public IBortableZone? PlaceholderZone { get; set; }

    /// <summary>Index within <see cref="PlaceholderZone"/> where the item would be inserted.</summary>
    public int PlaceholderIndex { get; set; }

    /// <summary>
    /// In swap mode, the zone that holds the item currently highlighted as the swap target (the item the
    /// dragged one will trade places with on drop), if any.
    /// </summary>
    public IBortableZone? SwapTargetZone { get; set; }

    /// <summary>In swap mode, the item highlighted as the swap target, if any.</summary>
    public object? SwapTargetItem { get; set; }

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
    private readonly Dictionary<IBortableZone, object?[]> _before = new(ReferenceEqualityComparer.Instance);

    /// <summary>The zones this drag has modified (see <see cref="Touch"/>).</summary>
    public IEnumerable<IBortableZone> TouchedZones => _before.Keys;

    /// <summary>Records the items of <paramref name="zone"/>; call it right before the drag modifies that zone.</summary>
    public void Touch(IBortableZone zone)
    {
        if (_before.ContainsKey(zone)) return;

        var items = new object?[zone.Count];
        for (int i = 0; i < items.Length; i++)
            items[i] = zone.ItemAt(i);
        _before[zone] = items;
    }

    /// <summary>Whether this drag left <paramref name="zone"/> in a different order than it found it.</summary>
    public bool Changed(IBortableZone zone)
    {
        if (!_before.TryGetValue(zone, out var before)) return false;
        if (before.Length != zone.Count) return true;
        for (int i = 0; i < before.Length; i++)
            if (!Equals(before[i], zone.ItemAt(i))) return true;
        return false;
    }

    /// <summary>Puts <paramref name="zone"/> back to the items it held before this drag first changed it.</summary>
    public void Restore(IBortableZone zone)
    {
        if (_before.TryGetValue(zone, out var before)) zone.SetItems(before);
    }

    /// <summary>Guards against finalizing the same drag twice (drop + dragend).</summary>
    public bool Finished { get; set; }

    /// <summary>
    /// What <c>CanDrop</c> threw when it was asked as the drag was committed (about a multi-drag's gathering): the
    /// drag is put back and reported as refused, and the exception is passed on after.
    /// </summary>
    public System.Runtime.ExceptionServices.ExceptionDispatchInfo? Failure { get; set; }
}
