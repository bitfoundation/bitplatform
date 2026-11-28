namespace Bit.BlazorUI;

/// <summary>One flattened row of the result list (header or item). Exposed to templates.</summary>
public sealed class BitCommandPaletteRow
{
    internal BitCommandPaletteRow(BitCommandPaletteRowKind kind, string? group, BitCommandPaletteMatch? match, int itemIndex, string elementId)
    {
        Kind = kind;
        Group = group;
        Match = match;
        ItemIndex = itemIndex;
        ElementId = elementId;
    }

    /// <summary>Whether the row is a group heading or an item.</summary>
    public BitCommandPaletteRowKind Kind { get; }

    /// <summary>Group heading text (for headers, and for items the group they belong to).</summary>
    public string? Group { get; }

    /// <summary>Match details for item rows.</summary>
    public BitCommandPaletteMatch? Match { get; }

    /// <summary>Zero-based index among item rows; -1 for headers.</summary>
    public int ItemIndex { get; }

    /// <summary>DOM id used for <c>aria-activedescendant</c>.</summary>
    public string ElementId { get; }

    /// <summary>Zero-based index among all rows, headings included.</summary>
    internal int Position { get; init; }

    /// <summary>The element id of the heading an item is listed under, or null for an item before any heading.</summary>
    internal string? GroupHeaderId { get; init; }

    /// <summary>The command of an item row. Throws for a heading.</summary>
    public BitCommandPaletteCommand Command => Match?.Command ?? throw new InvalidOperationException("Row has no command.");
}
