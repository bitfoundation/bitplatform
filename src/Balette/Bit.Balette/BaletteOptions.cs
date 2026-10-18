namespace Bit.Balette;

/// <summary>Global configuration, registered via <c>services.AddBitBaletteServices(o =&gt; ...)</c>.</summary>
public sealed class BaletteOptions
{
    /// <summary>Shortcut that toggles the palette. Default <c>Mod+K</c> (⌘K on macOS, Ctrl+K elsewhere).</summary>
    public BaletteKeyboardShortcut? ToggleShortcut { get; set; } = BaletteKeyboardShortcut.Parse("Mod+K");

    /// <summary>Additional toggle shortcuts (e.g. <c>Ctrl+Shift+P</c>, <c>/</c>).</summary>
    public List<BaletteKeyboardShortcut> AdditionalToggleShortcuts { get; } = [];

    /// <summary>Don't fire global command shortcuts while an input, textarea or contenteditable has focus.</summary>
    public bool SuppressShortcutsInEditableElements { get; set; } = true;

    /// <summary>Milliseconds a multi-chord sequence (<c>G I</c>) waits for the next chord.</summary>
    public int SequenceTimeoutMs { get; set; } = 1000;

    /// <summary>Debounce for async sources, in milliseconds.</summary>
    public int DebounceMs { get; set; } = 150;

    /// <summary>Wrap around when arrowing past the first/last item.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Enable <c>Ctrl+J</c>/<c>Ctrl+K</c> navigation inside the list (<c>Ctrl+N</c>/<c>Ctrl+P</c> are always on).</summary>
    public bool VimBindings { get; set; }

    /// <summary>Track recents and frecency.</summary>
    public bool EnableHistory { get; set; } = true;

    /// <summary>How many recent commands to show on the empty query.</summary>
    public int MaxRecents { get; set; } = 5;

    /// <summary>Upper bound of history entries kept.</summary>
    public int MaxHistoryEntries { get; set; } = 200;

    /// <summary>localStorage key for <see cref="BaletteBrowserStorageHistoryStore"/>.</summary>
    public string HistoryStorageKey { get; set; } = "bit-balette.history";

    /// <summary>Keep group headings while searching (cmdk style). When false, results are a flat ranked list.</summary>
    public bool GroupWhenSearching { get; set; } = true;

    /// <summary>Max rows rendered (groups excluded) for a query. Protects against huge registries.</summary>
    public int MaxResults { get; set; } = 150;

    /// <summary>Explicit group order. Groups not listed come after, alphabetically.</summary>
    public List<string> GroupOrder { get; } = [];

    /// <summary>Weight of <see cref="BaletteCommand.Priority"/> when a query is present (score points per priority unit).</summary>
    public double PriorityWeight { get; set; } = 0.25;

    /// <summary>Weight of frecency when a query is present.</summary>
    public double FrecencyWeight { get; set; } = 6;

    /// <summary>Show a "Modes" hint group on the empty query listing registered prefixes.</summary>
    public bool ShowModeHints { get; set; } = true;

    /// <summary>Close when clicking the backdrop.</summary>
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>Close when the app navigates (e.g. a handler called <c>NavigationManager.NavigateTo</c>).</summary>
    public bool CloseOnNavigation { get; set; } = true;

    /// <summary>Reset query and page stack when closed. When false, reopening restores the previous state.</summary>
    public bool ResetOnClose { get; set; } = true;

    /// <summary>Highlight matched characters in titles and subtitles.</summary>
    public bool HighlightMatches { get; set; } = true;

    /// <summary>Show the footer with keyboard hints.</summary>
    public bool ShowFooter { get; set; } = true;

    /// <summary>Render shortcut hints on rows.</summary>
    public bool ShowShortcuts { get; set; } = true;

    /// <summary>Use <c>&lt;Virtualize&gt;</c> for the list (recommended above ~500 rows). Requires fixed <see cref="RowHeight"/>.</summary>
    public bool Virtualize { get; set; }

    /// <summary>Row height in pixels used by virtualization.</summary>
    public float RowHeight { get; set; } = 44f;

    /// <summary>Text matcher used for scoring. Default <see cref="BaletteFuzzyMatcher"/>.</summary>
    public IBaletteMatcher Matcher { get; set; } = BaletteFuzzyMatcher.Default;

    /// <summary>Per-field weights for scoring.</summary>
    public BaletteMatchWeights Weights { get; } = new();

    /// <summary>UI strings. Subclass or set properties to localize.</summary>
    public BaletteStrings Strings { get; set; } = new();

    /// <summary>Commands registered at startup.</summary>
    public List<BaletteCommand> Commands { get; } = [];

    /// <summary>Modes registered at startup.</summary>
    public List<BaletteCommandMode> Modes { get; } = [];

    /// <summary>Async sources queried in the default mode.</summary>
    public List<IBaletteSource> Sources { get; } = [];

    /// <summary>Colour theme: <c>auto</c> (prefers-color-scheme), <c>light</c> or <c>dark</c>.</summary>
    public string Theme { get; set; } = "auto";
}
