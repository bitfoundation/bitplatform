namespace Bit.Balette;

/// <summary>Kind of a rendered row.</summary>
public enum BaletteRowKind
{
    GroupHeader,
    Item,
    Separator,
}

/// <summary>One flattened row of the result list (header or item). Exposed to templates.</summary>
public sealed class BaletteRow
{
    internal BaletteRow(BaletteRowKind kind, string? group, BaletteMatch? match, int itemIndex, string elementId)
    {
        Kind = kind;
        Group = group;
        Match = match;
        ItemIndex = itemIndex;
        ElementId = elementId;
    }

    /// <summary>Whether the row is a group heading or an item.</summary>
    public BaletteRowKind Kind { get; }

    /// <summary>Group heading text (for headers, and for items the group they belong to).</summary>
    public string? Group { get; }

    /// <summary>Match details for item rows.</summary>
    public BaletteMatch? Match { get; }

    /// <summary>Zero-based index among item rows; -1 for headers.</summary>
    public int ItemIndex { get; }

    /// <summary>DOM id used for <c>aria-activedescendant</c>.</summary>
    public string ElementId { get; }

    /// <summary>The command of an item row. Throws for a heading.</summary>
    public BaletteCommand Command => Match?.Command ?? throw new InvalidOperationException("Row has no command.");
}

/// <summary>Context handed to <c>ItemTemplate</c>.</summary>
public sealed record BaletteItemContext(BaletteRow Row, bool IsActive, bool IsChecked, bool IsPinned, bool IsConfirming, bool IsMac);

/// <summary>Snapshot of palette state handed to header/footer templates.</summary>
public sealed record BaletteStateContext(
    string Query,
    string? ModePrefix,
    string? ModeName,
    IReadOnlyList<string> Breadcrumbs,
    int ResultCount,
    int ActiveIndex,
    BaletteCommand? ActiveCommand,
    bool IsLoading,
    bool IsMultiSelect,
    int SelectedCount,
    string? Toast,
    bool IsMac);

/// <summary>A page on the navigation stack.</summary>
internal sealed class BalettePageFrame
{
    public required string Title { get; init; }

    /// <summary>The command whose children are listed (null for the root page and ad-hoc pages).</summary>
    public BaletteCommand? Command { get; init; }

    /// <summary>Explicit items for ad-hoc pages (results of <see cref="BaletteCommandResult.Page"/>, action panels, lazily loaded children).</summary>
    public IReadOnlyList<BaletteCommand>? Items { get; set; }

    public bool MultiSelect { get; init; }

    public Func<BaletteCommandContext, Task<BaletteCommandResult>>? OnSubmit { get; init; }

    public string? Placeholder { get; init; }

    public BaletteCommandResult.PromptResult? Prompt { get; init; }

    public bool IsActions { get; init; }

    /// <summary>Query to restore when this page is popped.</summary>
    public string QueryBefore { get; init; } = string.Empty;

    public HashSet<string> Selected { get; } = new(StringComparer.Ordinal);

    public bool IsRoot => Command is null && Items is null && Prompt is null;
}
