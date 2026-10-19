using Microsoft.AspNetCore.Components;

namespace Bit.Balette;

/// <summary>
/// Describes a single entry in the command palette: an action, a navigation target,
/// a toggle, or a container of nested commands. Commands are immutable; use <see cref="With"/> to derive copies.
/// </summary>
public class BaletteCommand
{
    private string? _explicitId;
    private string? _cachedId;

    /// <summary>
    /// Stable unique identifier. Defaults to a slug derived from <see cref="Title"/>, <see cref="Group"/> and <see cref="Parent"/>.
    /// Setting null keeps the derived id.
    /// </summary>
    public string Id
    {
        get => _explicitId ?? (_cachedId ??= BaletteCommandIdGenerator.FromTitle(Title, Group, Parent));
        init => _explicitId = string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>True when <see cref="Id"/> was set explicitly rather than derived.</summary>
    public bool HasExplicitId => _explicitId is not null;

    /// <summary>Primary label. Searched with the highest weight.</summary>
    public required string Title { get; init; }

    /// <summary>Secondary label rendered next to the title (e.g. a path or category).</summary>
    public string? Subtitle { get; init; }

    /// <summary>Longer explanation shown in the detail pane.</summary>
    public string? Description { get; init; }

    /// <summary>Alternative search terms (aliases, synonyms, abbreviations).</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>Group / section heading under which the command is listed.</summary>
    public string? Group { get; init; }

    /// <summary>Icon rendered before the title. Either this or <see cref="IconCss"/>.</summary>
    public RenderFragment? Icon { get; init; }

    /// <summary>CSS class used for an icon element (e.g. <c>bi bi-house</c>).</summary>
    public string? IconCss { get; init; }

    /// <summary>Keyboard shortcut. When <see cref="GlobalShortcut"/> is true, it also fires while the palette is closed.</summary>
    public BaletteKeyboardShortcut? Shortcut { get; init; }

    /// <summary>Whether <see cref="Shortcut"/> is registered globally on the document.</summary>
    public bool GlobalShortcut { get; init; } = true;

    /// <summary>Ranking hint. Higher values sort first when scores tie. See <see cref="BaletteCommandPriority"/>.</summary>
    public int Priority { get; init; } = BaletteCommandPriority.Normal;

    /// <summary>Id of the parent command. Commands with a parent are only visible inside that parent's page.</summary>
    public string? Parent { get; init; }

    /// <summary>Static nested commands. Selecting this command opens a page listing them.</summary>
    public IReadOnlyList<BaletteCommand>? Children { get; init; }

    /// <summary>Lazily resolved nested commands. Selecting this command opens a page listing them.</summary>
    public Func<BaletteCommandContext, CancellationToken, Task<IReadOnlyList<BaletteCommand>>>? ChildrenProvider { get; init; }

    /// <summary>Optional placeholder for the search box when this command's page is open.</summary>
    public string? ChildrenPlaceholder { get; init; }

    /// <summary>Allow checking several children before confirming with <c>Ctrl+Enter</c>.</summary>
    public bool MultiSelectChildren { get; init; }

    /// <summary>Secondary actions reachable via <c>Tab</c> / <c>→</c> (Raycast-style action panel).</summary>
    public IReadOnlyList<BaletteCommand>? Actions { get; init; }

    /// <summary>Handler executed when the command is selected.</summary>
    public Func<BaletteCommandContext, Task<BaletteCommandResult>>? Handler { get; init; }

    /// <summary>Navigation target. When set and <see cref="Handler"/> is null the palette navigates here.</summary>
    public string? Href { get; init; }

    /// <summary>Force navigation to reload the page (bypasses client routing).</summary>
    public bool ForceLoad { get; init; }

    /// <summary>Open <see cref="Href"/> in a new tab.</summary>
    public bool OpenInNewTab { get; init; }

    /// <summary>Semantic kind used for icons/badges and screen-reader hints.</summary>
    public BaletteCommandKind Kind { get; init; } = BaletteCommandKind.Action;

    /// <summary>Disabled commands are shown but cannot be executed.</summary>
    public bool Disabled { get; init; }

    /// <summary>Explanation rendered when the command is disabled.</summary>
    public string? DisabledReason { get; init; }

    /// <summary>Dynamic availability check evaluated when the list is built.</summary>
    public Func<bool>? IsVisible { get; init; }

    /// <summary>Marks the command as destructive (styled red).</summary>
    public bool Destructive { get; init; }

    /// <summary>When set, the user must confirm (press Enter twice) before the handler runs.</summary>
    public string? ConfirmMessage { get; init; }

    /// <summary>Small badge/pill rendered at the end of the row (e.g. "New", "Beta", "3").</summary>
    public string? Badge { get; init; }

    /// <summary>Current value for toggle/setting commands (e.g. "On", "Dark").</summary>
    public string? Value { get; init; }

    /// <summary>Checked state for <see cref="BaletteCommandKind.Toggle"/> commands.</summary>
    public bool? Checked { get; init; }

    /// <summary>Always show this command regardless of the query (cmdk <c>forceMount</c>).</summary>
    public bool AlwaysShow { get; init; }

    /// <summary>Exclude from the recents / frecency tracking.</summary>
    public bool ExcludeFromHistory { get; init; }

    /// <summary>Arbitrary data for templates and handlers.</summary>
    public object? Data { get; init; }

    /// <summary>Free-form tags, used by <see cref="BaletteCommandMode"/> scoping.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>True when this command opens a nested page.</summary>
    public bool HasChildren => Children is { Count: > 0 } || ChildrenProvider is not null;

    /// <summary>Returns a copy with changed values.</summary>
    public BaletteCommand With(Action<BaletteCommandBuilder> configure)
    {
        var b = new BaletteCommandBuilder(this);
        configure(b);
        return b.Build();
    }

    public override string ToString() => $"{Id}: {Title}";
}

/// <summary>Well-known priority levels (any int is allowed).</summary>
public static class BaletteCommandPriority
{
    /// <summary>Listed last; e.g. a feedback row.</summary>
    public const int Lowest = -100;
    /// <summary>Below the default.</summary>
    public const int Low = -50;
    /// <summary>The default.</summary>
    public const int Normal = 0;
    /// <summary>Above the default.</summary>
    public const int High = 50;
    /// <summary>Listed first.</summary>
    public const int Highest = 100;
}

/// <summary>Semantic kinds of commands.</summary>
public enum BaletteCommandKind
{
    Action,
    Navigation,
    Toggle,
    Setting,
    Search,
    Page,
    Help,
    Recent,
}

internal static class BaletteCommandIdGenerator
{
    public static string FromTitle(string title, string? group, string? parent)
    {
        var chars = title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        var prefixParts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            prefixParts.Add(parent.ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(group))
        {
            prefixParts.Add(group.ToLowerInvariant().Replace(' ', '-'));
        }

        return prefixParts.Count == 0 ? slug : string.Join('.', prefixParts) + "." + slug;
    }
}
