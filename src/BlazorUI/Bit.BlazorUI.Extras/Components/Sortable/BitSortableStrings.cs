namespace Bit.BlazorUI;

/// <summary>
/// All screen-reader-visible strings of a <see cref="BitSortable{TItem}"/>: the keyboard instructions every item points
/// to, how an item is described, and what is announced as an item is picked up, moved, dropped or put back. Assign a
/// customized instance to the list's <see cref="BitSortable{TItem}.Strings"/> parameter - or to a
/// <see cref="BitSortableParams"/> cascaded by a <see cref="BitParams"/> - to translate or reword them; every property
/// defaults to the English text. A list announces with the strings of the list a drag started in.
/// </summary>
/// <remarks>
/// The announcements are functions of a <see cref="BitSortableAnnouncement"/> rather than format strings, since what
/// they say depends on more than the values they put in (an item back where it was, one item selected or several).
/// </remarks>
public class BitSortableStrings
{
    /// <summary>The strings a list uses when it is given none.</summary>
    internal static BitSortableStrings Default { get; } = new();



    /// <summary>
    /// How to drag an item with the keyboard; every item refers to it (<c>aria-describedby</c>), so a screen reader reads
    /// it when an item is focused.
    /// </summary>
    public string Instructions { get; set; } =
        "To pick up the item, press Space or Enter. While it is picked up, the arrow keys move it, Home and End move it to " +
        "the start or the end of its list, Space or Enter drops it, and Escape puts it back.";

    /// <summary>Added to <see cref="Instructions"/> in a MultiDrag list: how to select several items from the keyboard.</summary>
    public string SelectionInstructions { get; set; } =
        "Shift and Space select or deselect the item; picking up a selected item carries the whole selection.";

    /// <summary>What an item is called in place of "list item" (<c>aria-roledescription</c>).</summary>
    public string RoleDescription { get; set; } = "sortable item";

    /// <summary>An item was picked up.</summary>
    public Func<BitSortableAnnouncement, string> PickedUp { get; set; } =
        a => $"Picked up {a.Item}. It is at position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>A picked-up item moved to a new position, or a move made by code moved an item.</summary>
    public Func<BitSortableAnnouncement, string> Moved { get; set; } =
        a => $"{a.Item} moved to position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>In swap mode, a picked-up item will trade places with <see cref="BitSortableAnnouncement.Target"/> when it is dropped.</summary>
    public Func<BitSortableAnnouncement, string> SwapTarget { get; set; } =
        a => a.Target is null ? $"{a.Item} will stay where it is." : $"{a.Item} will trade places with {a.Target}.";

    /// <summary>An item was dropped.</summary>
    public Func<BitSortableAnnouncement, string> Dropped { get; set; } =
        a => a.Returned
            ? $"Dropped {a.Item}. It is back at position {a.Position} of {a.Count}{In(a.List)}."
            : $"Dropped {a.Item} at position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>A drag was cancelled, or its drop refused, and the item put back.</summary>
    public Func<BitSortableAnnouncement, string> Canceled { get; set; } =
        a => $"Cancelled. {a.Item} is back at position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>A position the item was moved toward is refused (a rule, a full list), or the item cannot be picked up.</summary>
    public Func<BitSortableAnnouncement, string> Refused { get; set; } =
        a => $"{a.Item} cannot be moved there.";

    /// <summary>
    /// An item that cannot be dragged (one <see cref="BitSortable{TItem}.Filter"/> locks, or one of a disabled list) was
    /// asked to be picked up.
    /// </summary>
    public Func<BitSortableAnnouncement, string> Locked { get; set; } =
        a => $"{a.Item} cannot be moved.";

    /// <summary>In a MultiDrag list, Shift+Space added an item to the selection.</summary>
    public Func<BitSortableAnnouncement, string> Selected { get; set; } =
        a => $"{a.Item} selected. {a.Selected} {(a.Selected == 1 ? "item" : "items")} selected.";

    /// <summary>In a MultiDrag list, Shift+Space took an item out of the selection.</summary>
    public Func<BitSortableAnnouncement, string> Deselected { get; set; } =
        a => $"{a.Item} not selected. {a.Selected} {(a.Selected == 1 ? "item" : "items")} selected.";



    private static string In(string? list) => list is null ? "" : $" in {list}";
}
