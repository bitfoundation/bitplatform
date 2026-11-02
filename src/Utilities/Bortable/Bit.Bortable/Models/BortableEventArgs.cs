namespace Bit.Bortable;

/// <summary>
/// The payload of the drag lifecycle callbacks of <see cref="Bortable{TItem}"/> (<c>OnStart</c>,
/// <c>OnEnd</c>, <c>OnAdd</c>, <c>OnRemove</c>, <c>OnUpdate</c>, <c>OnSort</c> and <c>OnChange</c>).
/// Mirrors the SortableJS event object.
/// </summary>
/// <typeparam name="TItem">The item type bound to the list.</typeparam>
public sealed class BortableEventArgs<TItem>
{
    /// <summary>The item that was dragged.</summary>
    public TItem Item { get; init; } = default!;

    /// <summary>
    /// All items moved by this drag, in list order. For a multi-drag this is the whole selection
    /// (including <see cref="Item"/>); otherwise it contains just <see cref="Item"/>. Mirrors
    /// <c>evt.items</c> of the SortableJS MultiDrag plugin.
    /// </summary>
    public IReadOnlyList<TItem> Items { get; init; } = [];

    /// <summary>The item's index within its original list when the drag started.</summary>
    public int OldIndex { get; init; }

    /// <summary>The item's index within the destination list.</summary>
    public int NewIndex { get; init; }

    /// <summary>The <see cref="Bortable{TItem}.Id"/> of the source list.</summary>
    public string FromId { get; init; } = "";

    /// <summary>The <see cref="Bortable{TItem}.Id"/> of the destination list.</summary>
    public string ToId { get; init; } = "";

    /// <summary>The group name of the source list, if any.</summary>
    public string? FromGroup { get; init; }

    /// <summary>The group name of the destination list, if any.</summary>
    public string? ToGroup { get; init; }

    /// <summary><c>true</c> when the item crossed from one list into another.</summary>
    public bool CrossedLists => !string.Equals(FromId, ToId, StringComparison.Ordinal);

    /// <summary>How the drag was made: with a mouse, a finger, a pen, the keyboard, or by code.</summary>
    public BortableInput Input { get; init; }

    /// <summary>
    /// <c>true</c> when the move puts a copy of the item into another list (the source's
    /// <see cref="Bortable{TItem}.Pull"/> is <see cref="BortablePull.Clone"/>) and the original stays. <see cref="Item"/>
    /// is then the copy in the events that follow the drop, and the original in the ones before it.
    /// </summary>
    public bool IsClone { get; init; }

    /// <summary>
    /// Set on <c>OnEnd</c>: <c>true</c> when the drag ended without keeping any change, and every list it touched was
    /// put back as it was. It was cancelled (Escape); released outside every list of a source with
    /// <see cref="Bortable{TItem}.RevertOnSpill"/>; refused by the destination's <see cref="Bortable{TItem}.ConfirmDrop"/>
    /// (or one that threw); a multi-drag whose gathering on the drop the destination's
    /// <see cref="Bortable{TItem}.CanDrop"/> refused (or threw on); or a drag whose list left the page before it was
    /// dropped, into a list with a <see cref="Bortable{TItem}.ConfirmDrop"/>, which cannot be asked then.
    /// </summary>
    public bool Canceled { get; init; }
}
