using System.Collections;

namespace Bit.Bdnd;

/// <summary>
/// The texts Bdnd gives screen readers about a keyboard drag: the instructions every keyboard-draggable source is
/// described by, and the announcements a live region reads out as the drag is picked up, moved, dropped or
/// cancelled. Every text is replaceable - set the ones to translate or reword and keep the rest - and an
/// announcement that answers <c>null</c> or an empty string is not read out. Hand an instance to
/// <see cref="BdndProvider.Announcements"/> (or <see cref="BdndDragDropManager.Announcements"/>).
/// </summary>
public sealed class BdndAnnouncements
{
    /// <summary>The English texts every provider uses unless it is given others.</summary>
    public static BdndAnnouncements Default { get; } = new();

    /// <summary>
    /// How to drag with the keyboard. Every keyboard-draggable source (or its drag handle) references this text
    /// through <c>aria-describedby</c>, so a screen reader reads it when the source is focused. The keys are named by
    /// placeholders - <c>{PickUp}</c>, <c>{Drop}</c>, <c>{Cancel}</c>, <c>{Up}</c>, <c>{Down}</c>, <c>{Left}</c>,
    /// <c>{Right}</c>, <c>{First}</c>, <c>{Last}</c>, and <c>{Move}</c> for the four moves - which
    /// <see cref="NameKeys"/> fills in from the provider's <see cref="BdndKeyBindings"/>, so the text follows the keys
    /// bound. A text without placeholders is read as it is.
    /// </summary>
    public string Instructions { get; init; } =
        "To pick up, press {PickUp}. While dragging, use {Move} to move between drop targets, " +
        "{First} and {Last} to go to the first or last one, {Drop} to drop, and {Cancel} to cancel.";

    /// <summary>
    /// What a placeholder of <see cref="Instructions"/> reads (its name without the braces: <c>"PickUp"</c>,
    /// <c>"Move"</c>, ...) for these keys. The English default names each key (<c>"Space"</c>, <c>"Up Arrow"</c>,
    /// <c>"K"</c>) and joins a command's keys with "or"; <c>{Move}</c> is "the arrow keys" for the default moves, else
    /// each direction's keys ("Up Arrow or K for up, ..."). Replace it with <see cref="Instructions"/> to translate
    /// them.
    /// </summary>
    public Func<BdndKeyBindings, string, string> NameKeys { get; init; } = EnglishKeyNames;

    /// <summary>
    /// <see cref="Instructions"/> with its placeholders filled in for these keys - the text the sources are described by.
    /// </summary>
    internal string InstructionsFor(BdndKeyBindings keys)
    {
        var text = Instructions;
        if (text.Contains('{') is false) return text;
        foreach (var name in Placeholders)
        {
            var placeholder = "{" + name + "}";
            if (text.Contains(placeholder, StringComparison.Ordinal)) text = text.Replace(placeholder, NameKeys(keys, name), StringComparison.Ordinal);
        }
        return text;
    }

    private static readonly string[] Placeholders = ["PickUp", "Drop", "Cancel", "Move", "Up", "Down", "Left", "Right", "First", "Last"];

    /// <summary>The name of a source that has no <c>Label</c> and no text of its own.</summary>
    public string UnnamedSource { get; init; } = "item";

    /// <summary>The name of a drop target that has no <c>Label</c>: its place among the targets the item can be dropped on.</summary>
    public Func<int, int, string> UnnamedTarget { get; init; } =
        static (position, count) => $"drop target {position} of {count}";

    /// <summary>
    /// How many things a dragged item stands for (<see cref="BdndAnnouncement.ItemCount"/>). One by default, whatever
    /// the item - a column dragged as its list of cards is one column. A drag that carries a selection says so:
    /// <see cref="CountElements"/> counts the elements of an item that is a collection (a list, an array or a set of
    /// the selected cards), or count a selection object of your own.
    /// </summary>
    public Func<object?, int> CountItems { get; init; } = static _ => 1;

    /// <summary>
    /// The number of elements of an item that is a collection - an <see cref="ICollection"/> (lists, arrays) or an
    /// <see cref="IReadOnlyCollection{T}"/> of a reference type (a <c>HashSet&lt;string&gt;</c>, a set of records) -
    /// and one for anything else: the <see cref="CountItems"/> of a provider whose drags carry selections.
    /// </summary>
    public static int CountElements(object? item) => item switch
    {
        string => 1, // a string is one thing, though its characters are a collection
        ICollection collection => collection.Count,
        IReadOnlyCollection<object> collection => collection.Count,
        _ => 1,
    };

    /// <summary>Read when a keyboard drag begins.</summary>
    public Func<BdndAnnouncement, string?> DragStarted { get; init; } = static a =>
    {
        var picked = a.ItemCount > 1
            ? $"Picked up {a.SourceLabel} and {a.ItemCount - 1} other {(a.ItemCount == 2 ? "item" : "items")}."
            : $"Picked up {a.SourceLabel}.";
        return a.TargetCount switch
        {
            0 => $"{picked} There is no drop target for {(a.ItemCount > 1 ? "them" : "it")}; press Escape to cancel.",
            1 => $"{picked} 1 drop target is available.",
            var count => $"{picked} {count} drop targets are available.",
        };
    };

    /// <summary>Read when the keyboard moves the item onto another drop target, or to another edge of one.</summary>
    public Func<BdndAnnouncement, string?> DraggedOver { get; init; } = static a => a switch
    {
        { TargetLabel: null } => $"{Subject(a)} {Is(a)} not over a drop target.",
        { Before: true } => $"{Subject(a)} {Is(a)} before {a.TargetLabel}.",
        { Before: false } => $"{Subject(a)} {Is(a)} after {a.TargetLabel}.",
        _ => $"{Subject(a)} {Is(a)} over {a.TargetLabel}.",
    };

    /// <summary>Read when a keyboard move finds no further place in that direction; the drag stays where it is.</summary>
    public Func<BdndAnnouncement, string?> NoFurtherTarget { get; init; } = static a => a switch
    {
        { TargetLabel: null } => "There is no drop target in that direction.",
        { Before: true } => $"There is nothing further in that direction: {Subject(a)} {Stays(a)} before {a.TargetLabel}.",
        { Before: false } => $"There is nothing further in that direction: {Subject(a)} {Stays(a)} after {a.TargetLabel}.",
        _ => $"{a.TargetLabel} is the last drop target in that direction.",
    };

    /// <summary>Read when a keyboard drag ends with a drop on a target.</summary>
    public Func<BdndAnnouncement, string?> Dropped { get; init; } = static a => a.Before switch
    {
        true => $"{Subject(a)} {Was(a)} dropped before {a.TargetLabel}.",
        false => $"{Subject(a)} {Was(a)} dropped after {a.TargetLabel}.",
        _ => $"{Subject(a)} {Was(a)} dropped on {a.TargetLabel}.",
    };

    /// <summary>Read when a keyboard drag is cancelled, or dropped while it was over no target.</summary>
    public Func<BdndAnnouncement, string?> Cancelled { get; init; } = static a => $"Dragging {Subject(a)} was cancelled.";

    // the default NameKeys: the keys of a placeholder in English
    private static string EnglishKeyNames(BdndKeyBindings keys, string placeholder) => placeholder switch
    {
        "PickUp" => EnglishKeys(keys.PickUp),
        "Drop" => EnglishKeys(keys.Drop),
        "Cancel" => EnglishKeys(keys.Cancel),
        "Up" => EnglishKeys(keys.Up),
        "Down" => EnglishKeys(keys.Down),
        "Left" => EnglishKeys(keys.Left),
        "Right" => EnglishKeys(keys.Right),
        "First" => EnglishKeys(keys.First),
        "Last" => EnglishKeys(keys.Last),
        "Move" when keys.Up is ["ArrowUp"] && keys.Down is ["ArrowDown"] && keys.Left is ["ArrowLeft"] && keys.Right is ["ArrowRight"] => "the arrow keys",
        "Move" => EnglishList(new (IReadOnlyList<string> Keys, string Name)[] { (keys.Up, "up"), (keys.Down, "down"), (keys.Left, "left"), (keys.Right, "right") }
            .Where(direction => direction.Keys.Count > 0)
            .Select(direction => $"{EnglishKeys(direction.Keys)} for {direction.Name}")
            .ToList(), "and"),
        _ => placeholder,
    };

    // "Space or Enter": each key once, Esc being Escape
    private static string EnglishKeys(IReadOnlyList<string> keys) =>
        EnglishList(keys.Select(EnglishKey).Distinct(StringComparer.Ordinal).ToList(), "or");

    private static string EnglishKey(string key) => key switch
    {
        " " or "Spacebar" => "Space",
        "Esc" => "Escape",
        "ArrowUp" or "Up" => "Up Arrow",
        "ArrowDown" or "Down" => "Down Arrow",
        "ArrowLeft" or "Left" => "Left Arrow",
        "ArrowRight" or "Right" => "Right Arrow",
        "PageUp" => "Page Up",
        "PageDown" => "Page Down",
        { Length: 1 } => key.ToUpperInvariant(),
        _ => key,
    };

    // "A", "A or B", "A, B or C"
    private static string EnglishList(List<string> parts, string conjunction) => parts.Count switch
    {
        0 => "",
        1 => parts[0],
        _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} {conjunction} {parts[^1]}",
    };

    // what the English texts call the dragged item: its source's name, or how many things it carries
    private static string Subject(BdndAnnouncement a) => a.ItemCount > 1 ? $"{a.ItemCount} items" : a.SourceLabel;

    private static string Is(BdndAnnouncement a) => a.ItemCount > 1 ? "are" : "is";

    private static string Stays(BdndAnnouncement a) => a.ItemCount > 1 ? "stay" : "stays";

    private static string Was(BdndAnnouncement a) => a.ItemCount > 1 ? "were" : "was";
}

/// <summary>What an announcement of <see cref="BdndAnnouncements"/> is about.</summary>
public sealed class BdndAnnouncement
{
    /// <summary>
    /// The dragged source's name: its <c>Label</c>, else its <c>aria-label</c> or text, else
    /// <see cref="BdndAnnouncements.UnnamedSource"/>.
    /// </summary>
    public required string SourceLabel { get; init; }

    /// <summary>The name of the target the item is over (or was dropped on), or null when it is over none.</summary>
    public string? TargetLabel { get; init; }

    /// <summary>
    /// The edge of that target the item is placed against - <see cref="BdndEdge.Top"/> or <see cref="BdndEdge.Left"/>
    /// before it, <see cref="BdndEdge.Bottom"/> or <see cref="BdndEdge.Right"/> after it - or null when the target is
    /// one place (it lists no <see cref="BdndDropTarget{TItem}.Edges"/>).
    /// </summary>
    public BdndEdge? Edge { get; init; }

    /// <summary>
    /// Whether the item is placed before (true) or after (false) the target in its list's order - in a right-to-left
    /// list the right edge is before - or null when it is not placed against an edge.
    /// </summary>
    public bool? Before { get; init; }

    /// <summary>The 1-based place of that target among the targets the item can be dropped on; 0 when it is over none.</summary>
    public int TargetPosition { get; init; }

    /// <summary>How many targets the item can be dropped on.</summary>
    public int TargetCount { get; init; }

    /// <summary>The type of the dragged item.</summary>
    public required string ItemType { get; init; }

    /// <summary>The dragged item.</summary>
    public object? Item { get; init; }

    /// <summary>
    /// How many things the dragged item stands for, as <see cref="BdndAnnouncements.CountItems"/> counts them: more
    /// than one for a drag of several selected items, whose texts say "3 items" rather than the grabbed source's name.
    /// </summary>
    public int ItemCount { get; init; } = 1;
}
