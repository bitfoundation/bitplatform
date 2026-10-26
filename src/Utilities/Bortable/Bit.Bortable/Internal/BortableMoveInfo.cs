namespace Bit.Bortable;

/// <summary>
/// Carries the details of a move between zones so a zone can build a typed
/// <see cref="BortableEventArgs{TItem}"/> for its own item type.
/// </summary>
internal sealed class BortableMoveInfo
{
    public required object Item { get; init; }

    /// <summary>All items moved together (multi-drag); <c>null</c> means just <see cref="Item"/>.</summary>
    public IReadOnlyList<object>? Items { get; init; }

    public int OldIndex { get; init; }
    public int NewIndex { get; init; }
    public required IBortableZone From { get; init; }
    public required IBortableZone To { get; init; }

    public BortableInput Input { get; init; }

    /// <summary>A copy of the item goes into <see cref="To"/>, another list; the original stays.</summary>
    public bool IsClone { get; init; }

    /// <summary>The drag ended with every list it touched put back as it was.</summary>
    public bool Canceled { get; init; }
}
