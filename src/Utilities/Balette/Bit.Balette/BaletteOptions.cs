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

    /// <summary>Milliseconds a multi-chord sequence (<c>G I</c>) waits for the next chord. Must be positive.</summary>
    public int SequenceTimeoutMs
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = 1000;

    /// <summary>Debounce for async sources, in milliseconds. Must not be negative.</summary>
    public int DebounceMs
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 150;

    /// <summary>
    /// How long an async source may take, in milliseconds, before it counts as failed and its results are replaced by a
    /// retry row (default 10 000). 0 waits for ever. Must not be negative.
    /// </summary>
    public int SourceTimeoutMs
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 10_000;

    /// <summary>Wrap around when arrowing past the first/last item.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Enable <c>Ctrl+J</c>/<c>Ctrl+K</c> navigation inside the list (<c>Ctrl+N</c>/<c>Ctrl+P</c> are always on).</summary>
    public bool VimBindings { get; set; }

    /// <summary>Track recents and frecency.</summary>
    public bool EnableHistory { get; set; } = true;

    /// <summary>How many recent commands to show on the empty query. Must not be negative.</summary>
    public int MaxRecents
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 5;

    /// <summary>Upper bound of history entries kept; pinned commands are never pruned. Must not be negative.</summary>
    public int MaxHistoryEntries
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 200;

    /// <summary>localStorage key for <see cref="BaletteBrowserStorageHistoryStore"/>.</summary>
    public string HistoryStorageKey { get; set; } = "bit-balette.history";

    /// <summary>Keep group headings while searching (cmdk style). When false, results are a flat ranked list.</summary>
    public bool GroupWhenSearching { get; set; } = true;

    /// <summary>Max rows rendered (groups excluded) for a query. Protects against huge registries. Must be positive.</summary>
    public int MaxResults
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = 150;

    /// <summary>Explicit group order. Groups not listed come after, alphabetically.</summary>
    public List<string> GroupOrder { get; } = [];

    /// <summary>Weight of <see cref="BaletteCommand.Priority"/> when a query is present (score points per priority unit).</summary>
    public double PriorityWeight { get; set; } = 0.25;

    /// <summary>Weight of frecency when a query is present.</summary>
    public double FrecencyWeight { get; set; } = 6;

    /// <summary>
    /// Find nested commands from the root page too (default true), as kbar does: typing <c>dark</c> lists "Theme › Dark"
    /// without opening Theme first, and <c>theme dark</c> matches the ancestors' titles as well. Only commands registered
    /// with the service or declared in markup are found - a lazily loaded page's children do not exist until it opens -
    /// and only where every ancestor is visible and enabled and none is a multi-select page. Their recents and pins are
    /// listed on the root page as well.
    /// </summary>
    public bool SearchNestedCommands { get; set; } = true;

    /// <summary>
    /// When a query matches nothing, look again allowing a typo per term - one missing, extra, wrong or swapped letter in
    /// a term of 4 to 7 characters, two from 8 on - so <c>settigns</c> still finds "Settings" (default true). Only a query
    /// that found nothing is retried, so this never adds noise to one that works; the list says the results are close
    /// matches.
    /// </summary>
    public bool TypoTolerance { get; set; } = true;

    /// <summary>
    /// After the first chords of a shortcut sequence (<c>G</c> of <c>G S</c>), show and announce that the next key is
    /// awaited, as VS Code does in its status bar (default true). <see cref="IBaletteService.SequencePending"/> reports it
    /// to code either way.
    /// </summary>
    public bool ShowSequenceHint { get; set; } = true;

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

    /// <summary>Row height in pixels used by virtualization. Must be positive.</summary>
    public float RowHeight
    {
        get;
        set
        {
            if (!(value > 0) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The row height must be a positive number of pixels.");
            }

            field = value;
        }
    } = 44f;

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

    /// <summary>
    /// How much room rows, the search box and the footer take: <c>comfortable</c> (default) or <c>compact</c>, which suits
    /// a dense application or an inline palette. Exposed as <c>data-density</c>, so a stylesheet can add its own.
    /// </summary>
    public string Density { get; set; } = "comfortable";

    /// <summary>Colour theme: <c>auto</c> (prefers-color-scheme), <c>light</c> or <c>dark</c>.</summary>
    public string Theme { get; set; } = "auto";

    /// <summary>
    /// The URL schemes a command's <see cref="BaletteCommand.Href"/> may use; relative links are always allowed. A link
    /// with any other scheme - <c>javascript:</c>, <c>data:</c>, <c>vbscript:</c> - is refused and logged instead of
    /// followed, because commands often carry data from an API (see <see cref="IBaletteSource"/>). Default <c>http</c>,
    /// <c>https</c>, <c>mailto</c> and <c>tel</c>; compared case-insensitively.
    /// </summary>
    public HashSet<string> AllowedUrlSchemes { get; } = new(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto", "tel" };
}
