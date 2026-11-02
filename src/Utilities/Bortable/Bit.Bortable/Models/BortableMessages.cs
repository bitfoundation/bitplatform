namespace Bit.Bortable;

/// <summary>
/// The texts a <see cref="Bortable{TItem}"/> list gives assistive technology: the instructions every item points to,
/// how an item is described, and what is announced as an item is picked up, moved, dropped or put back. Replace them
/// to translate them or to word them for your app, through <see cref="Bortable{TItem}.Messages"/>. A list announces
/// with the messages of the list a drag started in.
/// </summary>
public class BortableMessages
{
    /// <summary>The messages a list uses when it is given none.</summary>
    public static BortableMessages Default { get; } = new();

    /// <summary>
    /// How to drag an item with the keyboard; every item refers to it (<c>aria-describedby</c>), so a screen reader reads
    /// it when an item is focused.
    /// </summary>
    public string Instructions { get; init; } =
        "To pick up the item, press Space or Enter. While it is picked up, the arrow keys move it, Home and End move it to " +
        "the start or the end of its list, Space or Enter drops it, and Escape puts it back.";

    /// <summary>Added to <see cref="Instructions"/> in a MultiDrag list: how to select several items from the keyboard.</summary>
    public string SelectionInstructions { get; init; } =
        "Shift and Space select or deselect the item; picking up a selected item carries the whole selection.";

    /// <summary>What an item is called in place of "list item" (<c>aria-roledescription</c>).</summary>
    public string RoleDescription { get; init; } = "sortable item";

    /// <summary>An item was picked up.</summary>
    public Func<BortableAnnouncement, string> PickedUp { get; init; } =
        a => $"Picked up {a.Item}. It is at position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>A picked-up item moved to a new position, or a move made by code moved an item.</summary>
    public Func<BortableAnnouncement, string> Moved { get; init; } =
        a => $"{a.Item} moved to position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>In swap mode, a picked-up item will trade places with <see cref="BortableAnnouncement.Target"/> when it is dropped.</summary>
    public Func<BortableAnnouncement, string> SwapTarget { get; init; } =
        a => a.Target is null ? $"{a.Item} will stay where it is." : $"{a.Item} will trade places with {a.Target}.";

    /// <summary>An item was dropped.</summary>
    public Func<BortableAnnouncement, string> Dropped { get; init; } =
        a => a.Returned
            ? $"Dropped {a.Item}. It is back at position {a.Position} of {a.Count}{In(a.List)}."
            : $"Dropped {a.Item} at position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>A drag was cancelled, or its drop refused, and the item put back.</summary>
    public Func<BortableAnnouncement, string> Canceled { get; init; } =
        a => $"Cancelled. {a.Item} is back at position {a.Position} of {a.Count}{In(a.List)}.";

    /// <summary>A position the item was moved toward is refused (a rule, a full list), or the item cannot be picked up.</summary>
    public Func<BortableAnnouncement, string> Refused { get; init; } =
        a => $"{a.Item} cannot be moved there.";

    /// <summary>An item that cannot be dragged (a <c>Filter</c>ed item, or one of a <c>Disabled</c> list) was asked to be picked up.</summary>
    public Func<BortableAnnouncement, string> Locked { get; init; } =
        a => $"{a.Item} cannot be moved.";

    /// <summary>In a MultiDrag list, Shift+Space added an item to the selection.</summary>
    public Func<BortableAnnouncement, string> Selected { get; init; } =
        a => $"{a.Item} selected. {a.Selected} {(a.Selected == 1 ? "item" : "items")} selected.";

    /// <summary>In a MultiDrag list, Shift+Space took an item out of the selection.</summary>
    public Func<BortableAnnouncement, string> Deselected { get; init; } =
        a => $"{a.Item} not selected. {a.Selected} {(a.Selected == 1 ? "item" : "items")} selected.";

    private static string In(string? list) => list is null ? "" : $" in {list}";
}

/// <summary>What a <see cref="BortableMessages"/> text is about: the item, and where it is.</summary>
public sealed class BortableAnnouncement
{
    /// <summary>The item, as <see cref="Bortable{TItem}.ItemLabel"/> names it.</summary>
    public required string Item { get; init; }

    /// <summary>Its position now, counted from 1.</summary>
    public int Position { get; init; }

    /// <summary>How many items the list it is in holds.</summary>
    public int Count { get; init; }

    /// <summary>The list it is in, as its <see cref="Bortable{TItem}.AriaLabel"/> names it; <c>null</c> when it has none.</summary>
    public string? List { get; init; }

    /// <summary>Its position when it was picked up, counted from 1.</summary>
    public int OldPosition { get; init; }

    /// <summary>The list it was picked up from, as its <see cref="Bortable{TItem}.AriaLabel"/> names it.</summary>
    public string? FromList { get; init; }

    /// <summary>
    /// Whether it is back where it was picked up: in the same list (not merely one with the same name, or none), at the
    /// same position.
    /// </summary>
    public bool Returned { get; init; }

    /// <summary>In swap mode, the item it will trade places with; otherwise <c>null</c>.</summary>
    public string? Target { get; init; }

    /// <summary>For <see cref="BortableMessages.Selected"/> and <see cref="BortableMessages.Deselected"/>: how many items are selected now.</summary>
    public int Selected { get; init; }
}
