namespace Bit.BlazorUI;

/// <summary>A page on the navigation stack.</summary>
internal sealed class BitCommandPalettePageFrame
{
    public required string Title { get; init; }

    /// <summary>The command whose children are listed (null for the root page and ad-hoc pages).</summary>
    public BitCommandPaletteCommand? Command { get; init; }

    /// <summary>Explicit items for ad-hoc pages (results of <see cref="BitCommandPaletteCommandResult.Page"/>, action panels, lazily loaded children).</summary>
    public IReadOnlyList<BitCommandPaletteCommand>? Items { get; set; }

    public bool MultiSelect { get; init; }

    public Func<BitCommandPaletteCommandContext, Task<BitCommandPaletteCommandResult>>? OnSubmit { get; init; }

    public string? Placeholder { get; init; }

    public BitCommandPaletteCommandResult.PromptResult? Prompt { get; init; }

    public bool IsActions { get; init; }

    /// <summary>
    /// For a page without a <see cref="Command"/>: the command that made it - the handler that returned a
    /// <see cref="BitCommandPaletteCommandResult.Page"/>, the row whose action panel it is. Its guard covers the page's rows.
    /// </summary>
    public BitCommandPaletteCommand? Owner { get; init; }

    /// <summary>
    /// Pushed by something outside the palette - a shortcut or code - on top of whatever page was open, which it has
    /// nothing to do with: the guards of the pages under it do not cover its rows.
    /// </summary>
    public bool Detached { get; set; }

    /// <summary>Query to restore when this page is popped.</summary>
    public string QueryBefore { get; init; } = string.Empty;

    public HashSet<string> Selected { get; } = new(StringComparer.Ordinal);

    /// <summary>The command's <see cref="BitCommandPaletteCommand.ChildSources"/>, searched as the user types on this page.</summary>
    public IReadOnlyList<IBitCommandPaletteSource>? Sources { get; init; }

    /// <summary>Every command the page's sources returned, by id: a multi-select page applies the checked ones across searches.</summary>
    public Dictionary<string, BitCommandPaletteCommand> SourceCommands { get; } = new(StringComparer.Ordinal);

    /// <summary>The command's <see cref="BitCommandPaletteCommand.ChildrenProvider"/> has not answered yet.</summary>
    public bool Loading { get; set; }

    /// <summary>The command's <see cref="BitCommandPaletteCommand.ChildrenProvider"/> threw; the page offers a retry.</summary>
    public bool LoadFailed { get; set; }

    /// <summary>Cancels the load in progress when the page is left, reset or loaded again.</summary>
    public CancellationTokenSource? LoadCts { get; set; }

    public bool IsRoot => Command is null && Items is null && Prompt is null;
}
