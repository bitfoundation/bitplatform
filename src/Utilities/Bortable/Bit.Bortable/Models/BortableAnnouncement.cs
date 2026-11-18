namespace Bit.Bortable;

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
