namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CommandPalette;

public partial class BitCommandPaletteDemo : IDisposable
{
    [Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

    // What the sections register with the service, removed when the page goes.
    private readonly List<IDisposable> registrations = [];

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The declarative BitCommandPaletteGroup and BitCommandPaletteOption children of the palette.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCommandPaletteClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the command palette.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Commands",
            Type = "IEnumerable<BitCommandPaletteCommand>?",
            DefaultValue = "null",
            Description = "Commands supplied directly to this palette, in addition to the ones registered through IBitCommandPaletteService and declared in its markup.",
        },
        new()
        {
            Name = "DetailTemplate",
            Type = "RenderFragment<BitCommandPaletteCommand>?",
            DefaultValue = "null",
            Description = "Renders a detail pane beside the list for the active command, Raycast style.",
        },
        new()
        {
            Name = "DisablePointerSelection",
            Type = "bool",
            DefaultValue = "false",
            Description = "Ignores the pointer hovering a row for choosing the active one: only the keys and a click move it.",
        },
        new()
        {
            Name = "EmptyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "What a page with no commands on it at all shows, in place of EmptyText.",
        },
        new()
        {
            Name = "EmptyText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text a page with no commands on it at all shows. Default: the Empty of the palette's strings, which falls back to BitStrings.NoItemsFound.",
        },
        new()
        {
            Name = "EnableHistory",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.EnableHistory: recents, pins and frecency.",
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment<BitCommandPaletteStateContext>?",
            DefaultValue = "null",
            Description = "Replaces the footer with its keyboard hints and toasts, with a snapshot of the palette's state.",
        },
        new()
        {
            Name = "GroupHeaderTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "Replaces the content of a group heading; receives the heading text.",
        },
        new()
        {
            Name = "GroupWhenSearching",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.GroupWhenSearching: keeps the group headings while a query is typed.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<BitCommandPaletteStateContext>?",
            DefaultValue = "null",
            Description = "Content rendered above the search box, with a snapshot of the palette's state.",
        },
        new()
        {
            Name = "HighlightMatches",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.HighlightMatches: marks the matched characters.",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment<BitCommandPaletteCommand>?",
            DefaultValue = "null",
            Description = "Renders the icon of every row; receives the command. Render nothing for no icon.",
        },
        new()
        {
            Name = "IncludeRegisteredCommands",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the commands, sources and modes registered through IBitCommandPaletteService are part of this palette. Set it to false for a palette that lists only its own, such as an inline search of one page's settings.",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the palette in the page instead of in a modal dialog. It is always open, Escape goes back a page or clears the query, and it leaves the toggle shortcut, the global shortcuts and the service's Open/Close/Execute to the modal palette. Set before the first render.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitCommandPaletteItemContext>?",
            DefaultValue = "null",
            Description = "Replaces the content of every row. The row element itself - its role, ARIA state and click handling - stays the palette's.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the loading state shown while a lazily loaded page or an async source has nothing to show yet.",
        },
        new()
        {
            Name = "Loop",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.Loop: the arrow keys wrap around the list.",
        },
        new()
        {
            Name = "NoResultsTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "What a search that matched nothing shows, in place of NoResultsText; receives the query.",
        },
        new()
        {
            Name = "NoResultsText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text a search that matched nothing shows. Default: the NoResults of the palette's strings, which falls back to BitStrings.NoResultsFound.",
        },
        new()
        {
            Name = "OnClosed",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Raised after the palette closed.",
        },
        new()
        {
            Name = "OnCommandExecuted",
            Type = "EventCallback<BitCommandPaletteCommandExecutedEventArgs>",
            DefaultValue = "",
            Description = "Raised after a command of this palette ran, with its result and how long the handler took.",
        },
        new()
        {
            Name = "OnOpened",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Raised after the palette opened.",
        },
        new()
        {
            Name = "OnQueryChanged",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised after every change of the query, with the raw text (a mode prefix included).",
        },
        new()
        {
            Name = "Open",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the modal palette is open. Two-way bindable.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder of the search box on the root page.",
        },
        new()
        {
            Name = "SearchNestedCommands",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.SearchNestedCommands: finds nested commands from the root page.",
        },
        new()
        {
            Name = "ShowFooter",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.ShowFooter.",
        },
        new()
        {
            Name = "ShowShortcuts",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.ShowShortcuts: shows each command's shortcut on its row.",
        },
        new()
        {
            Name = "Strings",
            Type = "BitCommandPaletteStrings?",
            DefaultValue = "null",
            Description = "The words of the palette, the ones only screen readers hear included. Default: BitCommandPaletteOptions.Strings.",
            LinkType = LinkType.Link,
            Href = "#strings",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCommandPaletteClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the command palette.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ToggleShortcut",
            Type = "string?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.ToggleShortcut (Mod+K), e.g. Ctrl+Shift+P.",
        },
        new()
        {
            Name = "TypoTolerance",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.TypoTolerance: retries a query that found nothing allowing a typo per term.",
        },
        new()
        {
            Name = "VimBindings",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.VimBindings: Ctrl+J / Ctrl+K move the selection.",
        },
        new()
        {
            Name = "Virtualize",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Overrides BitCommandPaletteOptions.Virtualize: renders the list through Virtualize.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCommandPaletteClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitCommandPalette: the modal dialog, or the section of an inline palette." },
                new() { Name = "Header", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header of the BitCommandPalette, which holds the breadcrumbs and the search box." },
                new() { Name = "Input", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the search box of the BitCommandPalette." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list of the BitCommandPalette." },
                new() { Name = "GroupHeading", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each group heading of the BitCommandPalette." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each row of the BitCommandPalette." },
                new() { Name = "ActiveItem", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the active row of the BitCommandPalette." },
                new() { Name = "Empty", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for what a page of the BitCommandPalette with no commands at all shows." },
                new() { Name = "NoResults", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for what a search of the BitCommandPalette that matched nothing shows." },
                new() { Name = "Footer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the footer of the BitCommandPalette, with its keyboard hints and toasts." },
                new() { Name = "Detail", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the detail pane of the BitCommandPalette." },
            ]
        },
        new()
        {
            Id = "strings",
            Title = "BitCommandPaletteStrings",
            Description = "Every word the palette says, the ones only screen readers hear included. Set its properties, or subclass it and override them (to read them from an IStringLocalizer), to localize. The templates are composite format strings: {0} is replaced.",
            Parameters =
            [
                new() { Name = "Placeholder", Type = "string", DefaultValue = "\"Type a command or search…\"", Description = "The search box placeholder on the root page." },
                new() { Name = "PromptPlaceholder", Type = "string", DefaultValue = "\"Type a value and press Enter…\"", Description = "The search box placeholder of a prompt that set none of its own." },
                new() { Name = "NoResults", Type = "string", DefaultValue = "BitStrings.Current.NoResultsFound", Description = "The state of a query that found nothing; {0} is the query. Default: NoResultsFound of Current, read when it is shown, so a BitStrings set later still reaches it." },
                new() { Name = "NoResultsHint", Type = "string", DefaultValue = "\"Try a different search, or check for typos.\"", Description = "The hint under NoResults." },
                new() { Name = "Empty", Type = "string", DefaultValue = "BitStrings.Current.NoItemsFound", Description = "The state of a page with nothing on it. Default: NoItemsFound of Current, read when it is shown, so a BitStrings set later still reaches it." },
                new() { Name = "Loading", Type = "string", DefaultValue = "\"Searching…\"", Description = "The loading state, shown and announced while a lazily loaded page or an async source has not answered." },
                new() { Name = "Recent", Type = "string", DefaultValue = "\"Recent\"", Description = "The heading of the recently used commands." },
                new() { Name = "Pinned", Type = "string", DefaultValue = "\"Pinned\"", Description = "The heading of the pinned commands, and what a pinned row says to screen readers." },
                new() { Name = "Suggestions", Type = "string", DefaultValue = "\"Suggestions\"", Description = "The heading of the ungrouped commands when pinned or recent ones are listed above them." },
                new() { Name = "Modes", Type = "string", DefaultValue = "\"Modes\"", Description = "The heading of the mode hints on the empty query." },
                new() { Name = "Actions", Type = "string", DefaultValue = "\"Actions\"", Description = "The search box placeholder of the action panel." },
                new() { Name = "Results", Type = "string", DefaultValue = "\"Results\"", Description = "The accessible name of the result list on the root page." },
                new() { Name = "Other", Type = "string", DefaultValue = "\"Other\"", Description = "Not used by the palette itself; kept for templates." },
                new() { Name = "Navigate", Type = "string", DefaultValue = "\"to navigate\"", Description = "Footer hint after the ↑ ↓ keys." },
                new() { Name = "Select", Type = "string", DefaultValue = "\"to select\"", Description = "Footer hint after the Enter key." },
                new() { Name = "Close", Type = "string", DefaultValue = "\"to close\"", Description = "Footer hint after the Esc key on the root page." },
                new() { Name = "ClearQuery", Type = "string", DefaultValue = "\"to clear\"", Description = "Footer hint after the Esc key in an inline palette with a query typed." },
                new() { Name = "Back", Type = "string", DefaultValue = "\"to go back\"", Description = "Footer hint after the Esc key on a nested page and in a prompt." },
                new() { Name = "OpenActions", Type = "string", DefaultValue = "\"for actions\"", Description = "Footer hint after the Tab key." },
                new() { Name = "Toggle", Type = "string", DefaultValue = "\"to toggle\"", Description = "Footer hint after the Enter key on a multi-select page." },
                new() { Name = "Apply", Type = "string", DefaultValue = "\"to apply\"", Description = "Footer hint after Ctrl+Enter on a multi-select page, and after Enter in a prompt." },
                new() { Name = "Pin", Type = "string", DefaultValue = "\"Pin to top\"", Description = "The action panel entry that pins a command." },
                new() { Name = "Unpin", Type = "string", DefaultValue = "\"Unpin\"", Description = "The action panel entry that unpins a command." },
                new() { Name = "RemoveFromRecents", Type = "string", DefaultValue = "\"Remove from recents\"", Description = "The action panel entry that forgets a command's uses." },
                new() { Name = "ClearRecents", Type = "string", DefaultValue = "\"Clear recent commands\"", Description = "The action panel entry that forgets every recent command (pins stay)." },
                new() { Name = "ConfirmPrefix", Type = "string", DefaultValue = "\"Confirm: \"", Description = "Put before the title of a command waiting for its confirming Enter." },
                new() { Name = "ConfirmHint", Type = "string", DefaultValue = "\"Press Enter again to confirm, Esc to cancel\"", Description = "Announced with the confirmation message of a command waiting for its second Enter." },
                new() { Name = "SelectedCount", Type = "string", DefaultValue = "\"{0} selected\"", Description = "How many rows are checked on a multi-select page; {0} is the count." },
                new() { Name = "ResultsAnnouncement", Type = "string", DefaultValue = "\"{0} results available\"", Description = "Announced after a search with several results; {0} is the count. See ResultsAnnouncementFor." },
                new() { Name = "OneResultAnnouncement", Type = "string", DefaultValue = "\"1 result available\"", Description = "Announced after a search with exactly one result." },
                new() { Name = "NoResultsAnnouncement", Type = "string", DefaultValue = "\"No results\"", Description = "Announced after a search with no result." },
                new() { Name = "PageAnnouncement", Type = "string", DefaultValue = "\"{0}. {1}\"", Description = "Announced when a page opens or the palette goes back to one; {0} is its title, {1} what it lists." },
                new() { Name = "Disabled", Type = "string", DefaultValue = "\"Unavailable\"", Description = "Shown when a disabled command without a reason is chosen." },
                new() { Name = "ModeHintTitle", Type = "string", DefaultValue = "\"{0} — {1}\"", Description = "Not used by the palette itself; kept for templates." },
                new() { Name = "DialogLabel", Type = "string", DefaultValue = "\"Command palette\"", Description = "The accessible name of the dialog." },
                new() { Name = "InputLabel", Type = "string", DefaultValue = "\"Search commands\"", Description = "The accessible name of the search box." },
                new() { Name = "CloseButton", Type = "string", DefaultValue = "\"Close\"", Description = "The accessible name of the close button." },
                new() { Name = "BackButton", Type = "string", DefaultValue = "\"Back\"", Description = "The accessible name of the back button." },
                new() { Name = "Breadcrumb", Type = "string", DefaultValue = "\"Breadcrumb\"", Description = "The accessible name of the breadcrumb navigation." },
                new() { Name = "OpenPalette", Type = "string", DefaultValue = "\"Open command palette\"", Description = "What BitCommandPaletteTrigger's accessible name says it does, after its visible label and TriggerNameSeparator (WCAG 2.5.3: the name starts with what is shown), and its whole name when the trigger shows no label. A string that already holds the label - a whole name such as \"Search commands\" for the label \"Search\" - is the name on its own, as is a label that already holds this string." },
                new() { Name = "TriggerNameSeparator", Type = "string", DefaultValue = "\", \"", Description = "What joins BitCommandPaletteTrigger's visible label and OpenPalette in its accessible name; a language with a comma of its own uses it (\"، \" in Persian and Arabic)." },
                new() { Name = "Search", Type = "string", DefaultValue = "\"Search\"", Description = "The label of BitCommandPaletteTrigger when it sets none of its own." },
                new() { Name = "Home", Type = "string", DefaultValue = "\"Home\"", Description = "The title of the root page." },
                new() { Name = "MoreResults", Type = "string", DefaultValue = "\"Show {0} more…\"", Description = "The row that loads a source's next page when it said how many results there are; {0} is how many are left." },
                new() { Name = "LoadMore", Type = "string", DefaultValue = "\"Show more…\"", Description = "The row that loads a source's next page when it did not say how many results there are." },
                new() { Name = "SequencePending", Type = "string", DefaultValue = "\"waiting for the next key…\"", Description = "Shown, and announced, after the first chords of a shortcut sequence (G of G S), until it completes or times out." },
                new() { Name = "CommandFailed", Type = "string", DefaultValue = "\"Couldn’t run “{0}”.\"", Description = "Shown when a command failed; {0} is its title. See Message." },
                new() { Name = "CommandRefused", Type = "string", DefaultValue = "\"You can’t run “{0}”.\"", Description = "Shown when a guard refused a command without a message of its own (see IBitCommandPaletteCommandGuard); {0} is its title." },
                new() { Name = "CommandUnavailable", Type = "string", DefaultValue = "\"“{0}” is no longer available.\"", Description = "Shown when a listed command was chosen after it stopped being available - its VisibleWhen, or an ancestor's, turned false since the list was built; {0} is its title." },
                new() { Name = "LoadFailed", Type = "string", DefaultValue = "\"Couldn’t load this list.\"", Description = "Shown when the children of a page failed to load." },
                new() { Name = "Retry", Type = "string", DefaultValue = "\"Try again\"", Description = "The button and hint that load a failed list again." },
                new() { Name = "ApproximateResults", Type = "string", DefaultValue = "\"No exact matches. Showing close ones.\"", Description = "Shown above results found only by allowing typos (see TypoTolerance)." },
                new() { Name = "ApproximateResultsAnnouncement", Type = "string", DefaultValue = "\"{0} {1}\"", Description = "The announcement of such results: {0} is ApproximateResults, {1} the result count." },
                new() { Name = "Running", Type = "string", DefaultValue = "\"Working…\"", Description = "Announced, and shown by a spinner, while a command's handler has been running for a moment." },
                new() { Name = "SourceFailed", Type = "string", DefaultValue = "\"Couldn’t search {0}\"", Description = "The row an async source that failed or timed out leaves; {0} is its name. Choosing it retries." },
                new() { Name = "On", Type = "string", DefaultValue = "\"On\"", Description = "What screen readers hear for a toggle that is on." },
                new() { Name = "Off", Type = "string", DefaultValue = "\"Off\"", Description = "What screen readers hear for a toggle that is off." },
                new() { Name = "ShortcutThen", Type = "string", DefaultValue = "\"then\"", Description = "The word between the chords of a sequence: \"G then S\"." },
            ]
        },
        new()
        {
            Id = "options",
            Title = "BitCommandPaletteOptions",
            Description = "The app-wide configuration, set through AddBitCommandPaletteServices(options => ...). The palette's own parameters, and a BitCommandPaletteParams cascaded through a BitParams, override what they share with it.",
            Parameters =
            [
                new() { Name = "ToggleShortcut", Type = "BitCommandPaletteKeyboardShortcut?", DefaultValue = "BitCommandPaletteKeyboardShortcut.Parse(\"Mod+K\")", Description = "Shortcut that toggles the palette. Default Mod+K (⌘K on macOS, Ctrl+K elsewhere)." },
                new() { Name = "AdditionalToggleShortcuts", Type = "List<BitCommandPaletteKeyboardShortcut>", DefaultValue = "[]", Description = "Additional toggle shortcuts (e.g. Ctrl+Shift+P, /)." },
                new() { Name = "SuppressShortcutsInEditableElements", Type = "bool", DefaultValue = "true", Description = "Don't fire global command shortcuts while an input, textarea or contenteditable has focus." },
                new() { Name = "Loop", Type = "bool", DefaultValue = "true", Description = "Wrap around when arrowing past the first/last item." },
                new() { Name = "VimBindings", Type = "bool", DefaultValue = "false", Description = "Enable Ctrl+J/Ctrl+K navigation inside the list (Ctrl+N/Ctrl+P are always on)." },
                new() { Name = "EnableHistory", Type = "bool", DefaultValue = "true", Description = "Track recents and frecency." },
                new() { Name = "HistoryStorageKey", Type = "string", DefaultValue = "\"bit-command-palette.history\"", Description = "localStorage key for BitCommandPaletteBrowserStorageHistoryStore." },
                new() { Name = "GroupWhenSearching", Type = "bool", DefaultValue = "true", Description = "Keep group headings while searching (cmdk style). When false, results are a flat ranked list." },
                new() { Name = "GroupOrder", Type = "List<string>", DefaultValue = "[]", Description = "Explicit group order. Groups not listed come after, alphabetically." },
                new() { Name = "PriorityWeight", Type = "double", DefaultValue = "0.25", Description = "Weight of Priority when a query is present (score points per priority unit)." },
                new() { Name = "FrecencyWeight", Type = "double", DefaultValue = "6", Description = "Weight of frecency when a query is present." },
                new() { Name = "SearchNestedCommands", Type = "bool", DefaultValue = "true", Description = "Find nested commands from the root page too (default true), as kbar does: typing dark lists \"Theme › Dark\" without opening Theme first, and theme dark matches the ancestors' titles as well. Only commands registered with the service or declared in markup are found - a lazily loaded page's children do not exist until it opens - and only where every ancestor is visible and enabled and none is a multi-select page. Their recents and pins are listed on the root page as well." },
                new() { Name = "TypoTolerance", Type = "bool", DefaultValue = "true", Description = "When a query matches nothing, look again allowing a typo per term - one missing, extra, wrong or swapped letter in a term of 4 to 7 characters, two from 8 on - so settigns still finds \"Settings\" (default true). Only a query that found nothing is retried, so this never adds noise to one that works; the list says the results are close matches." },
                new() { Name = "ShowSequenceHint", Type = "bool", DefaultValue = "true", Description = "After the first chords of a shortcut sequence (G of G S), show and announce that the next key is awaited, as VS Code does in its status bar (default true). SequencePending reports it to code either way." },
                new() { Name = "ShowModeHints", Type = "bool", DefaultValue = "true", Description = "Show a \"Modes\" hint group on the empty query listing registered prefixes." },
                new() { Name = "CloseOnBackdropClick", Type = "bool", DefaultValue = "true", Description = "Close when clicking the backdrop." },
                new() { Name = "CloseOnNavigation", Type = "bool", DefaultValue = "true", Description = "Close when the app navigates (e.g. a handler called NavigationManager.NavigateTo)." },
                new() { Name = "ResetOnClose", Type = "bool", DefaultValue = "true", Description = "Reset query and page stack when closed. When false, reopening restores the previous state." },
                new() { Name = "HighlightMatches", Type = "bool", DefaultValue = "true", Description = "Highlight matched characters in titles and subtitles." },
                new() { Name = "ShowFooter", Type = "bool", DefaultValue = "true", Description = "Show the footer with keyboard hints." },
                new() { Name = "ShowShortcuts", Type = "bool", DefaultValue = "true", Description = "Render shortcut hints on rows." },
                new() { Name = "Virtualize", Type = "bool", DefaultValue = "false", Description = "Use <Virtualize> for the list (recommended above ~500 rows). Requires fixed RowHeight." },
                new() { Name = "Matcher", Type = "IBitCommandPaletteMatcher", DefaultValue = "BitCommandPaletteFuzzyMatcher.Default", Description = "Text matcher used for scoring. Default BitCommandPaletteFuzzyMatcher." },
                new() { Name = "Weights", Type = "BitCommandPaletteMatchWeights", DefaultValue = "new()", Description = "Per-field weights for scoring." },
                new() { Name = "Strings", Type = "BitCommandPaletteStrings", DefaultValue = "new()", Description = "UI strings. Subclass or set properties to localize." },
                new() { Name = "Commands", Type = "List<BitCommandPaletteCommand>", DefaultValue = "[]", Description = "Commands registered at startup." },
                new() { Name = "Modes", Type = "List<BitCommandPaletteCommandMode>", DefaultValue = "[]", Description = "Modes registered at startup." },
                new() { Name = "Sources", Type = "List<IBitCommandPaletteSource>", DefaultValue = "[]", Description = "Async sources queried in the default mode." },
                new() { Name = "RecordCommandIdsInTraces", Type = "bool", DefaultValue = "true", Description = "Whether the bit.command_palette.command activities the palette traces carry the command's id (default true) - see BitCommandPaletteDiagnostics. Turn it off when ids embed personal data, as ids made from an API's records can (person:ada@example.com). Metrics never carry ids, and queries, prompt values and titles are never recorded. Off, it also keeps a command's id and title out of the palette's log messages about running it - a failure, a refusal, a guard that threw, an unsafe link - which usually leave through the same pipeline; they read (hidden) instead. The configuration warnings about shortcuts and duplicate ids, which name the commands an app registers, still carry them." },
                new() { Name = "AllowedUrlSchemes", Type = "HashSet<string>", DefaultValue = "new(StringComparer.OrdinalIgnoreCase) { \"http\", \"https\", \"mailto\", \"tel\" }", Description = "The URL schemes a command's Href may use; relative links are always allowed. A link with any other scheme - javascript:, data:, vbscript: - is refused and logged instead of followed, because commands often carry data from an API (see IBitCommandPaletteSource). Default http, https, mailto and tel; compared case-insensitively." },
                new() { Name = "SequenceTimeoutMs", Type = "int", DefaultValue = "1000", Description = "Milliseconds a multi-chord sequence (G I) waits for the next chord. Must be positive." },
                new() { Name = "DebounceMs", Type = "int", DefaultValue = "150", Description = "Debounce for async sources, in milliseconds. Must not be negative." },
                new() { Name = "SourceTimeoutMs", Type = "int", DefaultValue = "10000", Description = "How long an async source may take, in milliseconds, before it counts as failed and its results are replaced by a retry row. 0 waits for ever. Must not be negative." },
                new() { Name = "MaxRecents", Type = "int", DefaultValue = "5", Description = "How many recent commands to show on the empty query. Must not be negative." },
                new() { Name = "MaxHistoryEntries", Type = "int", DefaultValue = "200", Description = "Upper bound of history entries kept; pinned commands are never pruned. Must not be negative." },
                new() { Name = "MaxResults", Type = "int", DefaultValue = "150", Description = "Max rows rendered (groups excluded) for a query. Protects against huge registries. Must be positive." },
                new() { Name = "RowHeight", Type = "float", DefaultValue = "44", Description = "Row height in pixels used by virtualization. Must be positive." },
            ]
        },
        new()
        {
            Id = "trigger",
            Title = "BitCommandPaletteTrigger",
            Description = "A button that opens the modal palette - optionally with a query or on a command's page - and shows its toggle shortcut in the platform's own symbols.",
            Parameters =
            [
                new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "Replaces the button's content (icon, label and shortcut hint). Without a Label the button is named by what the content shows, followed by \", Open command palette\"; content that shows only an icon sets Label=\"\", so the name does not start with the separator." },
                new() { Name = "Classes", Type = "BitCommandPaletteTriggerClassStyles?", DefaultValue = "null", Description = "Custom CSS classes for the different parts of the trigger." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "The label. Default: \"Search\"; an empty one shows the icon alone. The button's accessible name starts with it. With ChildContent it is not shown, only named: set it to the text the content shows, or to an empty string for content that shows only an icon, and the button is named as the default one is." },
                new() { Name = "MacKeys", Type = "bool?", DefaultValue = "null", Description = "Forces the macOS key symbols on or off. Default: the platform the palette detected." },
                new() { Name = "PageCommandId", Type = "string?", DefaultValue = "null", Description = "Opens the palette directly inside the page of the command with this id." },
                new() { Name = "Query", Type = "string?", DefaultValue = "null", Description = "Opens the palette with this query (e.g. a mode prefix)." },
                new() { Name = "ShowShortcut", Type = "bool", DefaultValue = "true", Description = "Shows the toggle shortcut of the palette. Default: true." },
                new() { Name = "Styles", Type = "BitCommandPaletteTriggerClassStyles?", DefaultValue = "null", Description = "Custom CSS styles for the different parts of the trigger." },
            ]
        },
        new()
        {
            Id = "trigger-class-styles",
            Title = "BitCommandPaletteTriggerClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element (the button) of the BitCommandPaletteTrigger." },
                new() { Name = "Icon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the search icon of the BitCommandPaletteTrigger." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the BitCommandPaletteTrigger." },
                new() { Name = "Shortcut", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the shortcut hint of the BitCommandPaletteTrigger." },
            ]
        },
        new()
        {
            Id = "option",
            Title = "BitCommandPaletteOption",
            Description = "A command declared in markup inside a BitCommandPalette, directly or in a BitCommandPaletteGroup. Nested options turn it into a page.",
            Parameters =
            [
                new() { Name = "AlwaysShow", Type = "bool", DefaultValue = "false", Description = "Listed whatever the query, e.g. for a \"couldn't find it?\" row." },
                new() { Name = "Badge", Type = "string?", DefaultValue = "null", Description = "A small pill at the end of the row, e.g. \"New\" or \"Beta\"." },
                new() { Name = "Checked", Type = "bool?", DefaultValue = "null", Description = "The state of a toggle; renders a switch." },
                new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "Nested BitCommandPaletteOptions turn this item into a page." },
                new() { Name = "ChildrenPlaceholder", Type = "string?", DefaultValue = "null", Description = "Placeholder for the nested page when this item has child items." },
                new() { Name = "ConfirmMessage", Type = "string?", DefaultValue = "null", Description = "Asks for a second Enter before running, showing this message." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Longer text for the detail pane. Searched with a low weight." },
                new() { Name = "Destructive", Type = "bool", DefaultValue = "false", Description = "Styled as dangerous. With ConfirmMessage, Enter has to be pressed twice." },
                new() { Name = "Disabled", Type = "bool", DefaultValue = "false", Description = "Listed but not runnable." },
                new() { Name = "DisabledReason", Type = "string?", DefaultValue = "null", Description = "Why the item is disabled; shown instead of the subtitle and when it is selected." },
                new() { Name = "GlobalShortcut", Type = "bool", DefaultValue = "true", Description = "Whether Shortcut also fires while the palette is closed. Default true." },
                new() { Name = "Href", Type = "string?", DefaultValue = "null", Description = "Navigation target (used when OnExecute is not set)." },
                new() { Name = "Icon", Type = "RenderFragment?", DefaultValue = "null", Description = "An icon rendered before the title." },
                new() { Name = "IconCss", Type = "string?", DefaultValue = "null", Description = "A CSS class for an icon element, e.g. bi bi-house." },
                new() { Name = "Id", Type = "string?", DefaultValue = "null", Description = "Stable id. Defaults to a slug of Title." },
                new() { Name = "KeepOpen", Type = "bool", DefaultValue = "false", Description = "Keep the palette open after OnExecute." },
                new() { Name = "Keywords", Type = "string?", DefaultValue = "null", Description = "Comma or space separated keywords." },
                new() { Name = "Kind", Type = "BitCommandPaletteCommandKind?", DefaultValue = "null", Description = "The semantic kind; picks the default icon. Inferred when not set." },
                new() { Name = "MultiSelect", Type = "bool", DefaultValue = "false", Description = "Allow checking several children before applying with Ctrl+Enter (calls OnExecute with the selection)." },
                new() { Name = "NewTab", Type = "bool", DefaultValue = "false", Description = "Open Href in a new tab." },
                new() { Name = "OnExecute", Type = "EventCallback<BitCommandPaletteCommandContext>", DefaultValue = "", Description = "Invoked when the item is selected. The palette closes afterwards unless KeepOpen is set." },
                new() { Name = "Priority", Type = "int?", DefaultValue = "null", Description = "A ranking nudge; see BitCommandPaletteCommandPriority. Defaults to the group's priority." },
                new() { Name = "Shortcut", Type = "string?", DefaultValue = "null", Description = "Shortcut, e.g. Mod+Shift+D or G I." },
                new() { Name = "Subtitle", Type = "string?", DefaultValue = "null", Description = "A secondary label next to the title. Searched and highlighted." },
                new() { Name = "Tags", Type = "string[]?", DefaultValue = "null", Description = "Free-form labels that modes can scope to; combined with the group's." },
                new() { Name = "Title", Type = "string", DefaultValue = "string.Empty", Description = "The label. Searched with the highest weight and highlighted." },
                new() { Name = "Value", Type = "string?", DefaultValue = "null", Description = "The current value of a setting, e.g. \"Dark\"." },
            ]
        },
        new()
        {
            Id = "group",
            Title = "BitCommandPaletteGroup",
            Description = "Groups the BitCommandPaletteOptions declared inside it under one heading.",
            Parameters =
            [
                new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "The BitCommandPaletteOptions of the group." },
                new() { Name = "Heading", Type = "string", DefaultValue = "string.Empty", Description = "Group heading applied to all BitCommandPaletteOption children." },
                new() { Name = "Priority", Type = "int?", DefaultValue = "null", Description = "Priority applied to children that don't set their own." },
                new() { Name = "Tags", Type = "string[]?", DefaultValue = "null", Description = "Tags applied to all children (for mode scoping)." },
            ]
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "",
            Description = "Whether the dialog is open.",
        },
        new()
        {
            Name = "Query",
            Type = "string",
            DefaultValue = "",
            Description = "The current raw query, a mode prefix included.",
        },
        new()
        {
            Name = "ActiveCommand",
            Type = "BitCommandPaletteCommand?",
            DefaultValue = "",
            Description = "The command of the active row, if any.",
        },
        new()
        {
            Name = "OpenAsync",
            Type = "(string? query, string? pageCommandId) => Task",
            DefaultValue = "",
            Description = "Opens the palette, optionally with a query (a mode prefix is honoured) or directly on a command's page.",
        },
        new()
        {
            Name = "CloseAsync",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Closes the palette.",
        },
        new()
        {
            Name = "ToggleAsync",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Opens the palette when it is closed and closes it when it is open.",
        },
        new()
        {
            Name = "SetQueryAsync",
            Type = "(string query) => Task",
            DefaultValue = "",
            Description = "Sets the query as if typed, and renders the results.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-CommandPalette-width", DefaultValue = "640px", Description = "Width of the dialog (920px with a detail pane)." },
        new() { Name = "--bit-CommandPalette-max-height", DefaultValue = "min(70vh, 560px)", Description = "Tallest the list grows, with its header and footer." },
        new() { Name = "--bit-CommandPalette-top", DefaultValue = "12vh", Description = "Distance of the dialog from the top of the viewport." },
        new() { Name = "--bit-CommandPalette-radius", DefaultValue = "var(--bit-shp-radius-dialog)", Description = "Corner radius of the dialog." },
        new() { Name = "--bit-CommandPalette-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Fill of the dialog and of an inline palette." },
        new() { Name = "--bit-CommandPalette-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text of the palette." },
        new() { Name = "--bit-CommandPalette-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Lines between the header, the list and the footer, and the edge of an inline palette." },
        new() { Name = "--bit-CommandPalette-shadow", DefaultValue = "var(--bit-shd-dialog)", Description = "Elevation of the dialog." },
        new() { Name = "--bit-CommandPalette-backdrop", DefaultValue = "var(--bit-clr-bg-overlay)", Description = "Fill of the page behind the dialog." },
        new() { Name = "--bit-CommandPalette-row-height", DefaultValue = "var(--bit-siz-item-lg)", Description = "Least height of a row. Virtualize pins the rows to BitCommandPaletteOptions.RowHeight instead." },
        new() { Name = "--bit-CommandPalette-item-radius", DefaultValue = "var(--bit-shp-radius-md)", Description = "Corner radius of a row." },
        new() { Name = "--bit-CommandPalette-active-background", DefaultValue = "var(--bit-clr-bg-pri-hover)", Description = "Fill of the active row." },
        new() { Name = "--bit-CommandPalette-accent", DefaultValue = "var(--bit-clr-pri)", Description = "The mode chip, badges, checked rows, switches that are on and the busy spinner." },
        new() { Name = "--bit-CommandPalette-mark-background", DefaultValue = "var(--bit-clr-pri-tint)", Description = "Fill of the matched characters." },
        new() { Name = "--bit-CommandPalette-detail-width", DefaultValue = "40%", Description = "Width of the detail pane." },
        new() { Name = "--bit-CommandPalette-focus-color", DefaultValue = "the global focus ring", Description = "Focus ring of the palette's buttons." },
        new() { Name = "--bit-CommandPaletteTrigger-height", DefaultValue = "var(--bit-siz-ctrl-md)", Description = "Height of a BitCommandPaletteTrigger." },
        new() { Name = "--bit-CommandPaletteTrigger-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Fill of a BitCommandPaletteTrigger." },
        new() { Name = "--bit-CommandPaletteTrigger-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text and icon of a BitCommandPaletteTrigger." },
        new() { Name = "--bit-CommandPaletteTrigger-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Edge of a BitCommandPaletteTrigger." },
        new() { Name = "--bit-CommandPaletteTrigger-radius", DefaultValue = "var(--bit-shp-radius-control)", Description = "Corner radius of a BitCommandPaletteTrigger." },
        new() { Name = "--bit-CommandPaletteTrigger-focus-color", DefaultValue = "the global focus ring", Description = "Focus ring of a BitCommandPaletteTrigger." },
    ];



    protected override void OnInitialized()
    {
        AInitialize();
        BInitialize();
        CInitialize();
        DInitialize();
        EInitialize();
        FInitialize();

        base.OnInitialized();
    }

    public void Dispose()
    {
        foreach (var registration in registrations)
        {
            registration.Dispose();
        }
        BDispose();
    }



    private string? aLastRun;

    private bool aModalOpen;
    private bool aAltToggle;
    private int aOpenedCount;
    private int aClosedCount;
    private string? aModalLastRun;
    private BitCommandPaletteCommand[] aModalCommands = default!;

    // null leaves the toggle shortcut to the options: Mod+K.
    private string? aToggleShortcut => aAltToggle ? "Alt+K" : null;

    private void AInitialize()
    {
        aModalCommands =
        [
            BitCommandPaletteCmd.Action("Say hello", () => aModalLastRun = "Say hello")
                .Id("a-say-hello").Group("Actions").Keywords("greet", "hi"),
            BitCommandPaletteCmd.Page("Choose a greeting",
                BitCommandPaletteCmd.Action("Hola", () => aModalLastRun = "Hola"),
                BitCommandPaletteCmd.Action("Bonjour", () => aModalLastRun = "Bonjour"),
                BitCommandPaletteCmd.Action("Salam", () => aModalLastRun = "Salam"))
                .Id("a-greetings").Group("Actions"),
            BitCommandPaletteCmd.Navigate("Go to getting started", "/getting-started").Group("Navigation"),
        ];

        registrations.Add(paletteService.Register(
            BitCommandPaletteCmd.Action("Wave goodbye", () => aModalLastRun = "Wave goodbye")
                .Id("a-wave-goodbye").Group("Registered through the service")));
    }

    private Task AOpenWithQuery() => paletteService.OpenAsync("hello");

    private Task ARunSayHello() => paletteService.ExecuteAsync("a-say-hello");

    private const string BHelloId = "b-say-hello";

    private string bLastRun = "Nothing yet";
    private IDisposable? bHello;
    private BitCommandPaletteCommand[] bCommands = [];

    // Called from BInitialize (see the end of this draft for the full method).
    private void BBuildCommands()
    {
        RenderFragment star = builder => builder.AddMarkupContent(0, "<svg viewBox=\"0 0 16 16\" width=\"14\" height=\"14\" fill=\"currentColor\"><path d=\"M8 1.5l1.9 4 4.4.5-3.3 3 .9 4.3L8 11.1l-3.9 2.2.9-4.3-3.3-3 4.4-.5z\" /></svg>");

        bCommands =
        [
            BitCommandPaletteCmd.New("Export…")
                .Group("Builder")
                .Subtitle("A blank command, filled in step by step")
                .Description("New starts a command from its title; every other member has a builder method of the same name.")
                .Keywords("download", "save as")
                .Icon("bit-icon bit-icon--Download")
                .Handler(() => BRun("Export…")),
            BitCommandPaletteCmd.Action("Save", () => BRun("Save"))
                .Group("Builder")
                .Icon("bit-icon bit-icon--Save")
                .Shortcut("Mod+S", global: false),
            BitCommandPaletteCmd.Action("Reload", BReloadAsync)
                .Group("Builder")
                .Icon("bit-icon bit-icon--Refresh"),
            BitCommandPaletteCmd.Action("Rename…", ctx =>
                {
                    BRun($"Rename… (typed \"{ctx.Query}\")");
                    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
                })
                .Group("Builder")
                .Icon("bit-icon bit-icon--Rename"),
            BitCommandPaletteCmd.Page("Theme",
                    BitCommandPaletteCmd.Action("Light", () => BRun("Theme › Light")).Id("b-builder-light"),
                    BitCommandPaletteCmd.Action("Dark", () => BRun("Theme › Dark")).Id("b-builder-dark"))
                .Group("Builder")
                .ChildrenPlaceholder("Pick a theme…"),
            BitCommandPaletteCmd.Navigate("Getting started", "/getting-started")
                .Group("Links")
                .Keywords("install", "setup"),
            BitCommandPaletteCmd.New("bit platform on GitHub")
                .Href("https://github.com/bitfoundation/bitplatform", newTab: true)
                .Group("Links")
                .Keywords("source", "repository"),
            BitCommandPaletteCmd.New("What's new")
                .Group("Links")
                .Badge("New")
                .Icon(star)
                .Handler(() => BRun("What's new"))
                .Kind(BitCommandPaletteCommandKind.Help),
        ];
    }

    private void BRun(string title)
    {
        bLastRun = title;
        BRefresh();
    }

    private async Task BReloadAsync()
    {
        await Task.Delay(600);
        BRun("Reload (after 600 ms)");
    }

    private void BToggleHello()
    {
        if (bHello is not null)
        {
            bHello.Dispose();
            bHello = null;
            return;
        }

        bHello = paletteService.Register(
            BitCommandPaletteCmd.Action("Say hello", () => BRun("Say hello (registered from code)"))
                .Id(BHelloId)
                .Group("Registered from code")
                .Keywords("wave", "hi"));

        // Disposing a registration twice is harmless, so the page's Dispose can simply dispose all of them.
        registrations.Add(bHello);
    }

    private const string BGreetId = "b-greet";

    private string bGreetStatus = "Nothing yet";
    private IDisposable? bGreetOverride;

    private static readonly (string Built, string Id)[] bIdentityExamples =
    [
        ("New(\"Settings\").Group(\"Navigation\")", BitCommandPaletteCmd.New("Settings").Group("Navigation").Build().Id),
        ("New(\"Rename project…\").Group(\"Actions\")", BitCommandPaletteCmd.New("Rename project…").Group("Actions").Build().Id),
        ("New(\"What's new\").Group(\"Help\")", BitCommandPaletteCmd.New("What's new").Group("Help").Build().Id),
        ("New(\"Dark\").Parent(\"preferences.theme\")", BitCommandPaletteCmd.New("Dark").Parent("preferences.theme").Build().Id),
        ("New(\"C#\")", BitCommandPaletteCmd.New("C#").Build().Id),
        ("New(\"C++\")", BitCommandPaletteCmd.New("C++").Build().Id),
        ("New(\"🚀\")", BitCommandPaletteCmd.New("🚀").Build().Id),
        ("New(\"Word wrap\").Id(\"toggle:wordwrap\")", BitCommandPaletteCmd.New("Word wrap").Id("toggle:wordwrap").Build().Id),
        ("New(\"Word wrap\").Group(\"Editor\").Build().With(b => b.Title(\"Soft wrap\"))", BitCommandPaletteCmd.New("Word wrap").Group("Editor").Build().With(b => b.Title("Soft wrap")).Id),
    ];

    private BitCommandPaletteCommand BGreetCommand() =>
        BitCommandPaletteCmd.Action("Greet the team", () => BGreet("the first registration"))
            .Id(BGreetId)
            .Group("Identity")
            .Keywords("welcome");

    private void BToggleGreetOverride()
    {
        if (bGreetOverride is not null)
        {
            bGreetOverride.Dispose();
            bGreetOverride = null;
            return;
        }

        bGreetOverride = paletteService.Register(
            BitCommandPaletteCmd.Action("Greet this page's visitors", () => BGreet("the override"))
                .Id(BGreetId)
                .Group("Identity")
                .Keywords("welcome"));
        registrations.Add(bGreetOverride);
    }

    private void BGreet(string which)
    {
        bGreetStatus = which;
        BRefresh();
    }

    private bool bGroupWhenSearching = true;

    private static readonly BitCommandPaletteCommand[] bOrderingCommands =
    [
        BitCommandPaletteCmd.New("Search everywhere"),
        BitCommandPaletteCmd.New("Zoom in").Group("View"),
        BitCommandPaletteCmd.New("Zoom out").Group("View"),
        BitCommandPaletteCmd.New("Reset zoom").Group("View").Priority(BitCommandPaletteCommandPriority.Low),
        BitCommandPaletteCmd.New("Cut").Group("Edit"),
        BitCommandPaletteCmd.New("Copy").Group("Edit"),
        BitCommandPaletteCmd.New("Paste").Group("Edit"),
        BitCommandPaletteCmd.New("Undo").Group("Edit").Priority(BitCommandPaletteCommandPriority.High),
        BitCommandPaletteCmd.New("Send feedback").Group("Help").Priority(BitCommandPaletteCommandPriority.Lowest).AlwaysShow(),
        BitCommandPaletteCmd.New("Keyboard shortcuts").Group("Help"),
    ];

    private const string BFocusModeId = "b-focus-mode";
    private static readonly string[] bEditorThemes = ["Light", "Dark", "High contrast"];

    private bool bWordWrap = true;
    private bool bMinimap;
    private string bEditorTheme = "Light";
    private bool bFocusMode;
    private BitCommandPaletteCommand[] bSettings = [];

    private void BBuildSettings()
    {
        bSettings =
        [
            BitCommandPaletteCmd.New("Word wrap")
                .Id("b-setting-wordwrap")
                .Group("Editor")
                .Keywords("soft wrap", "lines")
                .Checked(bWordWrap)
                .Handler(_ =>
                {
                    bWordWrap = !bWordWrap;
                    BSettingsChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
                }),
            BitCommandPaletteCmd.New("Show minimap")
                .Id("b-setting-minimap")
                .Group("Editor")
                .Keywords("outline", "overview")
                .Checked(bMinimap)
                .Handler(_ =>
                {
                    bMinimap = !bMinimap;
                    BSettingsChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
                }),
            BitCommandPaletteCmd.New("Editor theme")
                .Id("b-setting-theme")
                .Group("Editor")
                .Keywords("appearance", "color scheme")
                .Value(bEditorTheme)
                .ChildrenPlaceholder("Pick a theme…")
                .Children(bEditorThemes.Select(BEditorThemeOption)),
        ];
    }

    private BitCommandPaletteCommand BEditorThemeOption(string theme) =>
        BitCommandPaletteCmd.New(theme)
            .Id($"b-editor-theme-{theme.ToLowerInvariant().Replace(' ', '-')}")
            .Checked(bEditorTheme == theme)
            .Handler(_ =>
            {
                bEditorTheme = theme;
                BSettingsChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Back);
            })
            .Build();

    private void BSetEditorTheme(string theme)
    {
        bEditorTheme = theme;
        BBuildSettings();
    }

    private void BSettingsChanged()
    {
        BBuildSettings();
        BRefresh();
    }

    private BitCommandPaletteCommand BFocusModeCommand() =>
        BitCommandPaletteCmd.New(bFocusMode ? "Leave focus mode" : "Enter focus mode")
            .Id(BFocusModeId)
            .Group("Editor")
            .Keywords("zen", "distraction free")
            .Checked(bFocusMode)
            .Handler(_ =>
            {
                BToggleFocusMode();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            });

    private void BToggleFocusMode()
    {
        bFocusMode = !bFocusMode;
        paletteService.Update(BFocusModeCommand());
        BRefresh();
    }

    private const string BMergeId = "b-merge-files";
    private static readonly string[] bFiles = ["README.md", "Program.cs", "App.razor", "site.css"];

    private readonly HashSet<string> bSelectedFiles = [];
    private string bAvailabilityStatus = "Nothing yet";
    private BitCommandPaletteCommand[] bAvailabilityCommands = [];

    private void BBuildAvailabilityCommands()
    {
        bAvailabilityCommands =
        [
            BMergeCommand(),
            BitCommandPaletteCmd.New("Export as PDF")
                .Group("Files")
                .Badge("Pro")
                .Icon("bit-icon bit-icon--Download")
                .Disabled(true, "Requires the Pro plan")
                .Handler(() => BAvailabilityRun("Exported as PDF")),
            BitCommandPaletteCmd.New("Publish the draft")
                .Group("Files")
                .Confirm("Publish the draft to everyone?")
                .Handler(() => BAvailabilityRun("Published the draft (not really)")),
            BitCommandPaletteCmd.New("Delete the draft")
                .Group("Danger zone")
                .Icon("bit-icon bit-icon--Delete")
                .Priority(BitCommandPaletteCommandPriority.Low)
                .Destructive("This permanently deletes the draft")
                .Handler(() => BAvailabilityRun("Deleted the draft (not really)")),
        ];
    }

    private BitCommandPaletteCommand BMergeCommand() =>
        BitCommandPaletteCmd.New("Merge selected files")
            .Id(BMergeId)
            .Group("Files")
            .Shortcut("Alt+M")
            .Keywords("combine", "join")
            .Icon("bit-icon bit-icon--Merge")
            .VisibleWhen(() => bSelectedFiles.Count >= 2)
            .Handler(() =>
            {
                var count = bSelectedFiles.Count;
                bSelectedFiles.Clear();
                paletteService.Refresh();
                BAvailabilityRun($"Merged {count} files (not really)");
            });

    private void BToggleFile(string file)
    {
        if (bSelectedFiles.Remove(file) is false)
        {
            bSelectedFiles.Add(file);
        }

        paletteService.Refresh();
    }

    private void BAvailabilityRun(string what)
    {
        bAvailabilityStatus = what;
        BRefresh();
    }

    private const string BIncrementId = "b-counter-increment";
    private const string BDecrementId = "b-counter-decrement";
    private const string BResetId = "b-counter-reset";
    private const string BZeroReason = "The counter is already at zero";

    private int bCount;
    private BitCommandPaletteCommand[] bPageCommands = [];

    private void BBuildPageCommands()
    {
        bPageCommands =
        [
            BitCommandPaletteCmd.New("Increment counter")
                .Id(BIncrementId)
                .Group("Counter")
                .Shortcut("Alt+Up")
                .Keywords("plus", "add")
                .Badge("0")
                .Icon("bit-icon bit-icon--Add")
                .Description("Registered by this page; gone when you leave it.")
                .Handler(_ => BStep(+1)),
            BitCommandPaletteCmd.New("Decrement counter")
                .Id(BDecrementId)
                .Group("Counter")
                .Shortcut("Alt+Down")
                .Keywords("minus", "subtract")
                .Badge("0")
                .Icon("bit-icon bit-icon--Remove")
                .Description("Registered by this page; gone when you leave it.")
                .Handler(_ => BStep(-1)),
            BitCommandPaletteCmd.New("Reset counter")
                .Id(BResetId)
                .Group("Counter")
                .Keywords("zero", "clear")
                .Destructive("Reset the counter to zero?")
                .Disabled(true, BZeroReason)
                .Handler(() => BSetCount(0)),
        ];
    }

    private Task<BitCommandPaletteCommandResult> BStep(int delta)
    {
        BSetCount(bCount + delta);
        return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
    }

    private void BSetCount(int count)
    {
        bCount = count;

        var badge = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var id in new[] { BIncrementId, BDecrementId })
        {
            if (paletteService.Find(id) is { } command)
            {
                paletteService.Update(command.With(b => b.Badge(badge)));
            }
        }

        if (paletteService.Find(BResetId) is { } reset)
        {
            paletteService.Update(reset.With(b => b.Disabled(count == 0, count == 0 ? BZeroReason : null)));
        }

        BRefresh();
    }

    private string bMarkupLastRun = "Nothing yet";
    private bool bMarkupWrap;
    private int bMarkupFontSize = 14;

    private void BApplyLabels(BitCommandPaletteCommandContext ctx)
    {
        bMarkupLastRun = ctx.SelectedCommands.Count == 0
            ? "No labels"
            : $"Labels: {string.Join(", ", ctx.SelectedCommands.Select(c => c.Title))}";
    }

    private bool bInlinePanelOpen = true;
    private string bInlineStatus = "Nothing yet";
    private BitCommandPaletteCommand[] bInlineCommands = [];

    private void BBuildInlineCommands()
    {
        bInlineCommands =
        [
            BitCommandPaletteCmd.Action("Show line numbers", () => BInlineRun("Show line numbers")).Group("View"),
            BitCommandPaletteCmd.Action("Fold all regions", () => BInlineRun("Fold all regions")).Group("View"),
            BitCommandPaletteCmd.Action("Format document", () => BInlineRun("Format document")).Group("Edit").Shortcut("Alt+Shift+F"),
            BitCommandPaletteCmd.Page("Go to symbol",
                    BitCommandPaletteCmd.Action("OnInitialized", () => BInlineRun("Went to OnInitialized")).Id("b-symbol-oninitialized"),
                    BitCommandPaletteCmd.Action("BuildRenderTree", () => BInlineRun("Went to BuildRenderTree")).Id("b-symbol-buildrendertree"),
                    BitCommandPaletteCmd.Action("Dispose", () => BInlineRun("Went to Dispose")).Id("b-symbol-dispose"))
                .Group("Navigate")
                .ChildrenPlaceholder("Filter symbols…"),
        ];
    }

    private void BInlineRun(string what)
    {
        bInlineStatus = what;
        BRefresh();
    }

    private void BInlinePanelKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            bInlinePanelOpen = false;
        }
    }

    private const string BFailId = "b-sync";
    private const string BRefusedId = "b-transfer";
    private const string BScratchId = "b-scratch-note";
    private static readonly BitCommandPaletteKeyboardShortcut bToggleShortcut = "Mod+K";

    private readonly List<string> bEvents = [];

    private BitCommandPaletteCommand BFailCommand() =>
        BitCommandPaletteCmd.New("Sync with the server")
            .Id(BFailId)
            .Group("Service")
            .Description("Fails on purpose, to show CommandFailed.")
            .Handler(async _ =>
            {
                await Task.Delay(300);
                throw new InvalidOperationException("The sync server is unreachable (on purpose).");
            });

    private static BitCommandPaletteCommand BRefusedCommand() =>
        BitCommandPaletteCmd.Action("Transfer ownership", () => { })
            .Id(BRefusedId)
            .Group("Service")
            .Description("Refused by its guard, to show CommandRefused.")
            .Guard(_ => false, "Only the owner can transfer the project");

    private BitCommandPaletteCommand BScratchCommand() =>
        BitCommandPaletteCmd.Action("Scratch note", () => BLog("Scratch note ran"))
            .Id(BScratchId)
            .Group("Service");

    private void BToggleScratch()
    {
        if (paletteService.Find(BScratchId) is null)
        {
            registrations.Add(paletteService.Register(BScratchCommand()));
            return;
        }

        BLog($"Unregister(\"{BScratchId}\") returned {paletteService.Unregister(BScratchId)}");
    }

    private void BOnCommandsChanged() => BLog($"CommandsChanged  {paletteService.Commands.Count} commands registered", collapse: true);

    private void BOnOpened() => BLog("Opened");

    private void BOnClosed() => BLog("Closed");

    private void BOnCommandExecuted(object? sender, BitCommandPaletteCommandExecutedEventArgs e)
        => BLog($"CommandExecuted  {e.Command.Id} → {e.Result.GetType().Name.Replace("Result", "", StringComparison.Ordinal)} in {e.Duration.TotalMilliseconds:0} ms");

    private void BOnCommandFailed(object? sender, BitCommandPaletteCommandErrorEventArgs e)
    {
        if (e.Command.Id == BFailId)
        {
            e.Message = "The server did not answer. Nothing was changed.";
            e.Handled = true;
        }

        BLog($"CommandFailed  {e.Command.Id}: {e.Exception.Message}");
    }

    private void BOnCommandRefused(object? sender, BitCommandPaletteCommandRefusedEventArgs e)
        => BLog($"CommandRefused  {e.Command.Id} ({e.Trigger}): {e.Message}");

    private void BOnPlatformDetected() => BLog($"PlatformDetected  IsMac = {paletteService.IsMac}");

    private void BOnSequencePending(BitCommandPaletteKeyboardShortcut? pending)
        => BLog(pending is null ? "SequencePending  (over)" : $"SequencePending  {pending.ToDisplayString(paletteService.IsMac)}…");

    private void BLog(string entry, bool collapse = false) => _ = InvokeAsync(() =>
    {
        if (collapse && bEvents.Count > 0 && bEvents[0].Contains("CommandsChanged", StringComparison.Ordinal))
        {
            bEvents.RemoveAt(0);
        }

        bEvents.Insert(0, $"{DateTime.Now:HH:mm:ss}  {entry}");
        if (bEvents.Count > 10)
        {
            bEvents.RemoveAt(bEvents.Count - 1);
        }

        StateHasChanged();
    });

    private void BRefresh() => _ = InvokeAsync(StateHasChanged);

    // Called from OnInitialized.
    private void BInitialize()
    {
        BBuildCommands();             // Commands from code
        BBuildSettings();             // Toggles and settings
        BBuildAvailabilityCommands(); // Disabled, hidden and destructive commands
        BBuildPageCommands();         // Page-scoped commands
        BBuildInlineCommands();       // Inline palettes

        // One registration for the commands the modal palette lists from these sections. Identity's override is a separate,
        // later registration, so it wins over BGreetCommand while it lives.
        registrations.Add(paletteService.Register(
            BGreetCommand(),      // Identity
            BFocusModeCommand(),  // Toggles and settings
            BMergeCommand(),      // Disabled, hidden and destructive commands
            BFailCommand(),       // The service
            BRefusedCommand(),    // The service
            BScratchCommand()));  // The service

        paletteService.CommandsChanged += BOnCommandsChanged;
        paletteService.Opened += BOnOpened;
        paletteService.Closed += BOnClosed;
        paletteService.CommandExecuted += BOnCommandExecuted;
        paletteService.CommandFailed += BOnCommandFailed;
        paletteService.CommandRefused += BOnCommandRefused;
        paletteService.PlatformDetected += BOnPlatformDetected;
        paletteService.SequencePending += BOnSequencePending;
    }

    // Called from the page's Dispose(), next to disposing `registrations`.
    private void BDispose()
    {
        paletteService.CommandsChanged -= BOnCommandsChanged;
        paletteService.Opened -= BOnOpened;
        paletteService.Closed -= BOnClosed;
        paletteService.CommandExecuted -= BOnCommandExecuted;
        paletteService.CommandFailed -= BOnCommandFailed;
        paletteService.CommandRefused -= BOnCommandRefused;
        paletteService.PlatformDetected -= BOnPlatformDetected;
        paletteService.SequencePending -= BOnSequencePending;
    }

    // ---- Cluster C: shared ------------------------------------------------------------------------

    [Inject] private BitCommandPaletteHistory cHistory { get; set; } = default!;

    /// <summary>Called from OnInitialized: sets up every section of cluster C.</summary>
    private void CInitialize()
    {
        CBuildResultCommands();
        CInitializeContext();
        CInitializePrompts();
        CInitializeMultiSelect();
        CInitializeLongRunning();
        CInitializeErrors();
        CInitializeGuards();
        CBuildNestedCommands();
        CInitializeNestedPages();
        CInitializeHistory();
    }

    /// <summary>Re-renders the page from an event that may arrive off the renderer's thread.</summary>
    private void CRefresh() => _ = InvokeAsync(StateHasChanged);

    /// <summary>Unsubscribes an event handler when the page's registrations are disposed.</summary>
    private sealed class CSubscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }

    private static readonly (string Name, string Role)[] cPeople =
    [
        ("Ada Lovelace", "Engineering"),
        ("Grace Hopper", "Engineering"),
        ("Alan Turing", "Research"),
        ("Katherine Johnson", "Data"),
        ("Margaret Hamilton", "Engineering"),
        ("Linus Torvalds", "Platform"),
        ("Barbara Liskov", "Architecture"),
        ("Dennis Ritchie", "Platform"),
        ("Hedy Lamarr", "Research"),
        ("Tim Berners-Lee", "Web"),
    ];

    // ---- Cluster C: Handlers and results ------------------------------------------------------------

    private string cResultStatus = "Nothing run yet.";
    private bool cWordWrap;
    private string cDensity = "Comfortable";
    private int cTodoCount;
    private readonly List<(string Id, string Text)> cTodos = [];
    private BitCommandPaletteCommand[] cResultCommands = [];

    private void CBuildResultCommands()
    {
        cResultCommands =
        [
            .. cTodos.Select(todo => BitCommandPaletteCmd.New(todo.Text)
                .Id(todo.Id)
                .Subtitle("To-do · Enter completes it (KeepOpen)")
                .ExcludeFromHistory()
                .Handler(_ =>
                {
                    cTodos.Remove(todo);
                    cResultStatus = $"Done: {todo.Text}";
                    CBuildResultCommands();
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
                })
                .Build()),

            BitCommandPaletteCmd.Action("Say hello", () => { cResultStatus = "Hello!"; StateHasChanged(); })
                .Subtitle("Close - an inline palette goes back to its root"),

            BitCommandPaletteCmd.New("Word wrap")
                .Subtitle("KeepOpen - the switch flips, the palette stays")
                .Checked(cWordWrap)
                .Handler(_ =>
                {
                    cWordWrap = !cWordWrap;
                    cResultStatus = $"Word wrap: {(cWordWrap ? "on" : "off")}";
                    CBuildResultCommands();
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
                }),

            BitCommandPaletteCmd.Page("Density", CDensityOption("Compact"), CDensityOption("Comfortable"), CDensityOption("Spacious"))
                .Subtitle("Back - each option returns to this list")
                .Value(cDensity),

            BitCommandPaletteCmd.New("Add a to-do…")
                .Subtitle("Prompt, then Reset")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt("New to-do", submit =>
                {
                    var text = submit.PromptValue!.Trim();
                    if (text.Length == 0)
                    {
                        return Task.FromResult(BitCommandPaletteCommandResult.Error("A to-do needs some text."));
                    }

                    cTodos.Add(($"c-todo-{++cTodoCount}", text));
                    cResultStatus = $"To-do added: {text}";
                    CBuildResultCommands();
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Reset);
                }, placeholder: "What needs doing?"))),

            BitCommandPaletteCmd.New("Check for updates")
                .Subtitle("Toast with keepOpen")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("You are on the latest version.", keepOpen: true))),

            BitCommandPaletteCmd.New("Save all")
                .Subtitle("Toast - then the palette closes")
                .Handler(_ =>
                {
                    cResultStatus = "All changes saved.";
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Toast("All changes saved."));
                }),

            BitCommandPaletteCmd.New("Join the beta")
                .Subtitle("Error")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Error("The beta is full. Try again next week."))),

            BitCommandPaletteCmd.New("Export…")
                .Subtitle("Page")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Page("Export format",
                [
                    BitCommandPaletteCmd.Action("CSV", () => CExported("CSV")).Subtitle("Comma separated values").Build(),
                    BitCommandPaletteCmd.Action("JSON", () => CExported("JSON")).Subtitle("Pretty printed").Build(),
                    BitCommandPaletteCmd.Action("PDF", () => CExported("PDF")).Subtitle("Print friendly").Badge("Pro").Disabled(true, "Requires the Pro plan").Build(),
                ], placeholder: "Pick a format…"))),
        ];
    }

    private BitCommandPaletteCommand CDensityOption(string density) =>
        BitCommandPaletteCmd.New(density)
            .Id($"c-density-{density.ToLowerInvariant()}")
            .Checked(cDensity == density)
            .Handler(_ =>
            {
                cDensity = density;
                cResultStatus = $"Density: {density}";
                CBuildResultCommands();
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.Back);
            });

    private void CExported(string format)
    {
        cResultStatus = $"Exported as {format}";
        StateHasChanged();
    }

    // ---- Cluster C: The command context -------------------------------------------------------------

    private readonly List<(string Member, string Value)> cContextLines = [];
    private BitCommandPaletteCommand[] cContextCommands = [];

    private void CInitializeContext()
    {
        cContextCommands =
        [
            BitCommandPaletteCmd.New("Inspect the context")
                .Keywords("ctx", "handler")
                .Handler(ctx =>
                {
                    cContextLines.Clear();
                    cContextLines.Add(("Command", $"{ctx.Command.Title} ({ctx.Command.Id})"));
                    cContextLines.Add(("Query", ctx.Query.Length == 0 ? "(empty)" : $"\"{ctx.Query}\""));
                    cContextLines.Add(("ModePrefix", ctx.ModePrefix ?? "null - the default mode"));
                    cContextLines.Add(("PromptValue", ctx.PromptValue ?? "null - not a prompt step"));
                    cContextLines.Add(("SelectedCommands", $"{ctx.SelectedCommands.Count} - not a multi-select page"));
                    cContextLines.Add(("GetRequiredService<IBitCommandPaletteService>()", ctx.GetRequiredService<IBitCommandPaletteService>() == ctx.Palette ? "the same service as Palette" : "another service"));
                    cContextLines.Add(("Navigation.Uri", ctx.Navigation.Uri));
                    cContextLines.Add(("Palette.Commands", $"{ctx.Palette.Commands.Count} registered commands"));
                    cContextLines.Add(("CancellationToken.CanBeCanceled", ctx.CancellationToken.CanBeCanceled.ToString()));
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
                }),

            BitCommandPaletteCmd.New("Feedback: couldn't find a command?")
                .AlwaysShow()
                .Priority(BitCommandPaletteCommandPriority.Lowest)
                .Handler(ctx => Task.FromResult(BitCommandPaletteCommandResult.Toast(
                    ctx.Query.Length > 0 ? $"Thanks! We logged “{ctx.Query}”." : "Thanks for the feedback!", keepOpen: true))),
        ];
    }

    // ---- Cluster C: Prompts -------------------------------------------------------------------------

    private string cProjectName = "Apollo";
    private string cPromptStatus = "Nothing submitted yet.";
    private static readonly string[] cMembers = ["ada@example.com", "grace@example.com"];
    private BitCommandPaletteCommand[] cPromptCommands = [];

    private void CInitializePrompts()
    {
        cPromptCommands =
        [
            BitCommandPaletteCmd.New("Add a note…")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt("New note", submit =>
                {
                    cPromptStatus = $"Note: {submit.PromptValue}";
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Toast("Note added"));
                }))),

            BitCommandPaletteCmd.New("Rename project…")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(
                    "New project name",
                    submit =>
                    {
                        var name = submit.PromptValue!.Trim();
                        if (name.Length == 0)
                        {
                            return Task.FromResult(BitCommandPaletteCommandResult.Error("A project needs a name."));
                        }

                        cProjectName = name;
                        cPromptStatus = $"Renamed to {name}";
                        StateHasChanged();
                        return Task.FromResult(BitCommandPaletteCommandResult.Toast($"Renamed to “{name}”"));
                    },
                    placeholder: "Project name…",
                    initialValue: cProjectName))),

            BitCommandPaletteCmd.New("Invite by e-mail…")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(
                    "E-mail address",
                    async submit =>
                    {
                        var email = submit.PromptValue!.Trim();
                        await Task.Delay(400, submit.CancellationToken);
                        if (cMembers.Contains(email, StringComparer.OrdinalIgnoreCase))
                        {
                            return BitCommandPaletteCommandResult.Error($"{email} is already a member.");
                        }

                        cPromptStatus = $"Invited {email}";
                        StateHasChanged();
                        return BitCommandPaletteCommandResult.Toast($"Invitation sent to {email}");
                    },
                    validate: value => System.Net.Mail.MailAddress.TryCreate(value.Trim(), out var parsedAddress) ? null : "Enter an e-mail address, like name@example.com.",
                    placeholder: "name@example.com"))),
        ];
    }

    // ---- Cluster C: Multi-select --------------------------------------------------------------------

    private string cMultiStatus = "Nothing applied yet.";
    private BitCommandPaletteCommand[] cMultiCommands = [];
    private static readonly string[] cWebhookEvents = ["push", "pull_request", "issues", "release", "deployment"];

    private void CInitializeMultiSelect()
    {
        cMultiCommands =
        [
            BitCommandPaletteCmd.Page("Set labels…",
                    BitCommandPaletteCmd.New("bug").Id("c-label-bug"),
                    BitCommandPaletteCmd.New("enhancement").Id("c-label-enhancement"),
                    BitCommandPaletteCmd.New("documentation").Id("c-label-documentation"),
                    BitCommandPaletteCmd.New("performance").Id("c-label-performance"))
                .MultiSelect()
                .ChildrenPlaceholder("Check labels, Ctrl+Enter to apply…")
                .Handler(ctx =>
                {
                    var labels = ctx.SelectedCommands.Select(c => c.Title).ToList();
                    cMultiStatus = labels.Count == 0 ? "Labels cleared." : $"Labels: {string.Join(", ", labels)}";
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Toast(labels.Count == 1 ? "1 label applied" : $"{labels.Count} labels applied"));
                }),

            BitCommandPaletteCmd.New("Invite people…")
                .MultiSelect()
                .ChildrenPlaceholder("Search people, Enter checks, Ctrl+Enter invites…")
                .Source("People", async (query, cancellationToken) =>
                {
                    await Task.Delay(250, cancellationToken);
                    return cPeople
                        .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Role.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Select(p => BitCommandPaletteCmd.New(p.Name).Id($"c-invite-{p.Name.Replace(' ', '-').ToLowerInvariant()}").Subtitle(p.Role).Build())
                        .ToList();
                })
                .Handler(ctx =>
                {
                    cMultiStatus = $"Invited: {string.Join(", ", ctx.SelectedCommands.Select(c => c.Title))}";
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Toast(ctx.SelectedCommands.Count == 1 ? "1 person invited" : $"{ctx.SelectedCommands.Count} people invited"));
                }),

            BitCommandPaletteCmd.New("Create webhook…")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Prompt(
                    "Payload URL",
                    address => Task.FromResult(BitCommandPaletteCommandResult.Page(
                        "Events to send",
                        [.. cWebhookEvents.Select(e => BitCommandPaletteCmd.New(e).Id($"c-event-{e}").Build())],
                        placeholder: "Check events, Ctrl+Enter to create…",
                        multiSelect: true,
                        onSubmit: picked =>
                        {
                            var events = picked.SelectedCommands.Select(c => c.Title).ToList();
                            if (events.Count == 0)
                            {
                                return Task.FromResult(BitCommandPaletteCommandResult.Error("Pick at least one event."));
                            }

                            cMultiStatus = $"Webhook to {address.PromptValue!.Trim()} for {string.Join(", ", events)}";
                            StateHasChanged();
                            return Task.FromResult(BitCommandPaletteCommandResult.Toast(events.Count == 1 ? "Webhook created for 1 event" : $"Webhook created for {events.Count} events"));
                        })),
                    validate: value => Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && uri.Host.Length > 0
                        ? null
                        : "Enter an https:// address.",
                    placeholder: "https://example.com/hooks",
                    initialValue: "https://"))),
        ];
    }

    // ---- Cluster C: Long-running commands -----------------------------------------------------------

    private string cReportStatus = "Not run yet.";

    private void CInitializeLongRunning()
    {
        registrations.Add(paletteService.Register(
            BitCommandPaletteCmd.New("Generate report…")
                .Id("c-report")
                .Group("Long-running")
                .Keywords("slow", "busy")
                .Handler(async ctx =>
                {
                    cReportStatus = "Generating…";
                    StateHasChanged();
                    try
                    {
                        await Task.Delay(3000, ctx.CancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cReportStatus = "Cancelled: the palette was closed while the report was being generated.";
                        StateHasChanged();
                        throw;
                    }

                    cReportStatus = "Report generated.";
                    StateHasChanged();
                    return BitCommandPaletteCommandResult.Toast("Report ready", keepOpen: true);
                })));
    }

    // ---- Cluster C: Errors --------------------------------------------------------------------------

    private bool cHandleFailures = true;
    private string cExecuteStatus = "";
    private readonly List<string> cFailures = [];
    private BitCommandPaletteCommand[] cErrorCommands = [];

    private void CInitializeErrors()
    {
        cErrorCommands =
        [
            BitCommandPaletteCmd.New("Sync with server")
                .Id("c-sync")
                .Group("Errors")
                .Handler(async ctx =>
                {
                    await Task.Delay(300, ctx.CancellationToken);
                    throw new InvalidOperationException("Connection to db-prod-7.internal:5432 refused for user 'svc_sync'.");
                }),

            BitCommandPaletteCmd.New("Publish release")
                .Id("c-publish")
                .Group("Errors")
                .Handler(async ctx =>
                {
                    await Task.Delay(300, ctx.CancellationToken);
                    throw new System.ComponentModel.DataAnnotations.ValidationException("The changelog is empty: write one before publishing.");
                }),
        ];

        registrations.Add(paletteService.Register(cErrorCommands));

        paletteService.CommandFailed += COnCommandFailed;
        registrations.Add(new CSubscription(() => paletteService.CommandFailed -= COnCommandFailed));
    }

    private void COnCommandFailed(object? sender, BitCommandPaletteCommandErrorEventArgs e)
    {
        if (e.Command.Id is not ("c-sync" or "c-publish" or "c-deployments"))
        {
            return;
        }

        if (e.Exception is System.ComponentModel.DataAnnotations.ValidationException)
        {
            e.Message = e.Exception.Message;
        }

        e.Handled = cHandleFailures;

        var shown = e.Command.Id == "c-deployments" ? "the page's error state, with Try again" : $"“{e.Message}”";
        cFailures.Insert(0, $"{DateTime.Now:HH:mm:ss}  {e.Command.Title}: {e.Exception.GetType().Name}, shown as {shown}");
        if (cFailures.Count > 6)
        {
            cFailures.RemoveAt(cFailures.Count - 1);
        }

        CRefresh();
    }

    private async Task CRunFromCodeAsync(string commandId)
    {
        try
        {
            await paletteService.ExecuteAsync(commandId);
            cExecuteStatus = $"ExecuteAsync(\"{commandId}\") returned normally: the subscriber handled the failure.";
        }
        catch (Exception ex)
        {
            cExecuteStatus = $"ExecuteAsync(\"{commandId}\") threw {ex.GetType().Name}.";
        }
    }

    // ---- Cluster C: Guards --------------------------------------------------------------------------

    private bool cIsPro;
    private bool cIsAdmin;
    private string cGuardStatus = "Nothing run yet.";
    private readonly List<string> cRefusals = [];
    private BitCommandPaletteCommand[] cGuardCommands = [];

    private void CInitializeGuards()
    {
        cGuardCommands =
        [
            BitCommandPaletteCmd.New("Export as PDF")
                .Id("c-guard-export")
                .Group("Guards")
                .Shortcut("G X")
                .Guard(_ => cIsPro, "Exports need the Pro plan.")
                .Handler(_ =>
                {
                    cGuardStatus = "Exported as PDF.";
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.Toast("Exported as PDF"));
                }),

            BitCommandPaletteCmd.New("Admin")
                .Id("c-guard-admin")
                .Group("Guards")
                .Guard(async (ctx, cancellationToken) =>
                {
                    await Task.Delay(400, cancellationToken);
                    return cIsAdmin ? BitCommandPaletteGuardResult.Allow : BitCommandPaletteGuardResult.Deny("Admins only.");
                })
                .Children(
                    BitCommandPaletteCmd.Action("Purge the cache", () => { cGuardStatus = "Cache purged."; StateHasChanged(); }).Id("c-guard-admin-purge"),
                    BitCommandPaletteCmd.Action("Read the audit log", () => { cGuardStatus = "Audit log opened."; StateHasChanged(); }).Id("c-guard-admin-audit")),
        ];

        registrations.Add(paletteService.Register(cGuardCommands));

        paletteService.CommandRefused += COnCommandRefused;
        registrations.Add(new CSubscription(() => paletteService.CommandRefused -= COnCommandRefused));
    }

    private void COnCommandRefused(object? sender, BitCommandPaletteCommandRefusedEventArgs e)
    {
        if (!e.Command.Id.StartsWith("c-guard-", StringComparison.Ordinal))
        {
            return;
        }

        cRefusals.Insert(0, $"{DateTime.Now:HH:mm:ss}  {e.Command.Title}, from {e.Trigger}: {e.Message ?? "(the default message)"}");
        if (cRefusals.Count > 6)
        {
            cRefusals.RemoveAt(cRefusals.Count - 1);
        }

        CRefresh();
    }

    // ---- Cluster C: Nested pages --------------------------------------------------------------------

    private string cNestedStatus = "Nothing picked yet.";
    private string cTheme = "Light";
    private string cLanguage = "English";
    private string cEditorTheme = "Light";
    private int cDeploymentLoads;
    private BitCommandPaletteCommand[] cNestedCommands = [];

    private static readonly string[] cLanguages = ["English", "Deutsch", "Français", "Español"];
    private static readonly string[] cBranches = ["main", "develop", "release/10.4", "feature/command-palette"];

    private static readonly (string Version, string Environment, string When)[] cDeployments =
    [
        ("v4.2.1", "Production", "2 hours ago"),
        ("v4.2.1", "Staging", "5 hours ago"),
        ("v4.2.0", "Production", "yesterday"),
        ("v4.1.9", "Production", "last week"),
    ];

    private void CInitializeNestedPages()
    {
        registrations.Add(paletteService.Register(
            BitCommandPaletteCmd.New("Preferences")
                .Id("c-prefs")
                .Group("Nested pages")
                .Children(
                    BitCommandPaletteCmd.New("Editor theme")
                        .Id("c-editor-theme")
                        .ChildrenPlaceholder("Pick an editor theme…")
                        .Children(CEditorThemeOption("Light"), CEditorThemeOption("Dark"), CEditorThemeOption("Solarized")),
                    BitCommandPaletteCmd.Action("Reset the editor", () => { cEditorTheme = "Light"; StateHasChanged(); })
                        .Id("c-prefs-reset"))));
    }

    private void CBuildNestedCommands()
    {
        cNestedCommands =
        [
            BitCommandPaletteCmd.New("Language")
                .Value(cLanguage)
                .ChildrenPlaceholder("Pick a language…")
                .Children(cLanguages.Select(language => BitCommandPaletteCmd.New(language)
                    .Id($"c-language-{language.ToLowerInvariant()}")
                    .Checked(cLanguage == language)
                    .Handler(_ =>
                    {
                        cLanguage = language;
                        cNestedStatus = $"Language: {language}";
                        CBuildNestedCommands();
                        StateHasChanged();
                        return Task.FromResult(BitCommandPaletteCommandResult.Back);
                    })
                    .Build())),

            BitCommandPaletteCmd.New("Assign to…")
                .Children(async (ctx, cancellationToken) =>
                {
                    await Task.Delay(400, cancellationToken);
                    return cPeople.Select(p => BitCommandPaletteCmd.New(p.Name)
                            .Subtitle(p.Role)
                            .Handler(() => { cNestedStatus = $"Assigned to {p.Name}"; StateHasChanged(); })
                            .Build())
                        .ToList();
                }, placeholder: "Pick an assignee…"),

            BitCommandPaletteCmd.New("Recent deployments…")
                .Id("c-deployments")
                .Children(async (ctx, cancellationToken) =>
                {
                    await Task.Delay(500, cancellationToken);
                    if (++cDeploymentLoads % 2 == 1)
                    {
                        throw new System.Net.Http.HttpRequestException("503 Service Unavailable from deploy-api.internal");
                    }

                    return cDeployments.Select(d => BitCommandPaletteCmd.New($"{d.Version} to {d.Environment}")
                            .Subtitle(d.When)
                            .ExcludeFromHistory()
                            .Handler(() => { cNestedStatus = $"Opened {d.Version} on {d.Environment}"; StateHasChanged(); })
                            .Build())
                        .ToList();
                }, placeholder: "Pick a deployment…"),

            BitCommandPaletteCmd.New("Switch branch…")
                .Handler(async ctx =>
                {
                    await Task.Delay(300, ctx.CancellationToken);
                    return BitCommandPaletteCommandResult.Page("Switch branch",
                        [.. cBranches.Select(branch => BitCommandPaletteCmd.Action(branch, () => { cNestedStatus = $"Switched to {branch}"; StateHasChanged(); }).Build())],
                        placeholder: "Pick a branch…");
                }),
        ];
    }

    private void CSetTheme(string theme)
    {
        cTheme = theme;
        cNestedStatus = $"Theme: {theme}";
    }

    private BitCommandPaletteCommand CEditorThemeOption(string theme) =>
        BitCommandPaletteCmd.New(theme)
            .Id($"c-editor-theme-{theme.ToLowerInvariant()}")
            .Handler(() => { cEditorTheme = theme; StateHasChanged(); });

    // ---- Cluster C: Recents, pins and the action panel ----------------------------------------------

    private bool cEnableHistory = true;
    private string cHistoryStatus = "Nothing run yet.";
    private BitCommandPaletteCommand[] cHistoryCommands = [];

    private void CInitializeHistory()
    {
        cHistoryCommands =
        [
            CPersonCommand("c-history-ada", "Ada Lovelace", "Engineering"),
            CPersonCommand("c-history-grace", "Grace Hopper", "Engineering"),
            CPersonCommand("c-history-alan", "Alan Turing", "Research"),
            BitCommandPaletteCmd.New("Start a timer")
                .Id("c-history-timer")
                .Handler(_ => CHistoryRun("Timer started")),
            BitCommandPaletteCmd.Navigate("Go to the Button demo", "/components/button")
                .Id("c-history-button")
                .Subtitle("A link: Ctrl+Enter opens it in a new tab"),
            BitCommandPaletteCmd.New("Scratch note")
                .Id("c-history-scratch")
                .Subtitle("ExcludeFromHistory: never recent, no action panel")
                .ExcludeFromHistory()
                .Handler(_ => CHistoryRun("Scratch note opened")),
        ];

        cHistory.Changed += COnHistoryChanged;
        registrations.Add(new CSubscription(() => cHistory.Changed -= COnHistoryChanged));
    }

    private BitCommandPaletteCommand CPersonCommand(string id, string name, string role) =>
        BitCommandPaletteCmd.New(name)
            .Id(id)
            .Subtitle(role)
            .Handler(_ => CHistoryRun($"Opened {name}'s profile"))
            .Actions(
                BitCommandPaletteCmd.New("Send an e-mail").Id($"{id}-email").ExcludeFromHistory().Handler(_ => CHistoryRun($"E-mail to {name} drafted")),
                BitCommandPaletteCmd.New("Mention in an issue").Id($"{id}-mention").ExcludeFromHistory().Handler(_ => CHistoryRun($"{name} mentioned")));

    private Task<BitCommandPaletteCommandResult> CHistoryRun(string message)
    {
        cHistoryStatus = message;
        StateHasChanged();
        return Task.FromResult(BitCommandPaletteCommandResult.Toast(message, keepOpen: true));
    }

    private string CHistoryText(IReadOnlyList<string> ids, string none)
    {
        var titles = ids.Select(id => cHistoryCommands.FirstOrDefault(c => c.Id == id))
                        .OfType<BitCommandPaletteCommand>()
                        .Select(c => $"{c.Title} ({cHistory.Get(c.Id)?.Count ?? 0}×)")
                        .ToList();
        return titles.Count == 0 ? none : string.Join(", ", titles);
    }

    private void COnHistoryChanged() => CRefresh();

    [Inject] private IServiceProvider dServices { get; set; } = default!;

    // Called from OnInitialized.
    private void DInitialize()
    {
        DLabQueryChanged(string.Empty);
        DRegisterModes();
        DRegisterSources();
        DRegisterPackages();
        DBuildPageSourceCommands();
        DBuildPerfCommands();
    }

    // The ranker the palette uses, over the default options: score, then priority and frecency.
    private static readonly BitCommandPaletteRanker dRanker = new(new BitCommandPaletteOptions());

    private static readonly BitCommandPaletteCommand[] dLabCommands =
    [
        BitCommandPaletteCmd.New("Go to settings").Id("d-lab-settings"),
        BitCommandPaletteCmd.New("Git: stash changes").Id("d-lab-stash"),
        BitCommandPaletteCmd.New("Toggle sidebar").Id("d-lab-sidebar"),
        BitCommandPaletteCmd.New("FuzzyMatcher.cs").Id("d-lab-fuzzy"),
        BitCommandPaletteCmd.New("Format document").Id("d-lab-format"),
        BitCommandPaletteCmd.New("Open recent file").Id("d-lab-recent"),
        BitCommandPaletteCmd.New("Reload window").Id("d-lab-reload"),
        BitCommandPaletteCmd.New("Rename symbol").Id("d-lab-rename"),
        BitCommandPaletteCmd.New("MCP server").Id("d-lab-mcp"),
    ];

    private string dLabQuery = string.Empty;
    private List<BitCommandPaletteMatch> dLabMatches = [];

    private void DLabQueryChanged(string query)
    {
        dLabQuery = query;

        // What the palette does on every keystroke: parse the query once, match every command, rank the matches.
        var parsed = BitCommandPaletteQuery.Parse(query);
        var matches = new List<BitCommandPaletteMatch>();
        foreach (var command in dLabCommands)
        {
            if (BitCommandPaletteFuzzyMatcher.Default.Match(command, parsed) is { } match)
            {
                matches.Add(match);
            }
        }

        // No frecency here, as the palette has EnableHistory="false".
        dLabMatches = dRanker.Rank(matches, hasQuery: parsed.Terms.Count > 0, frecencyOf: _ => 0);
    }

    private static string DFormat(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    // Stored exactly as written: accents, Arabic-script letters, ASCII digits.
    private static readonly BitCommandPaletteCommand[] dFoldCommands =
    [
        BitCommandPaletteCmd.New("Café").Id("d-fold-cafe"),
        BitCommandPaletteCmd.New("Łódź").Id("d-fold-lodz"),
        BitCommandPaletteCmd.New("Türkçe").Id("d-fold-turkce"),
        BitCommandPaletteCmd.New("علي").Id("d-fold-ali"),
        BitCommandPaletteCmd.New("فارسی").Id("d-fold-farsi"),
        BitCommandPaletteCmd.New("كتاب").Id("d-fold-ketab"),
        BitCommandPaletteCmd.New("إسم").Id("d-fold-esm"),
        BitCommandPaletteCmd.New("العربية").Id("d-fold-arabic"),
        BitCommandPaletteCmd.New("Order 12").Id("d-fold-order"),
        BitCommandPaletteCmd.New("Step 3").Id("d-fold-step"),
    ];

    private static readonly (string Typed, string Why)[] dFoldExamples =
    [
        ("cafe", "Latin letters lose their diacritics."),
        ("café", "A term that types a diacritic means that letter, and is matched as written."),
        ("lodz", "Letters with a stroke fold too: Ł is L, as in fzf."),
        ("turkce", "ü and ç are u and c."),
        ("علی", "Typed with the Persian yeh (ی), stored with the Arabic one (ي)."),
        ("فارسي", "The other way round: typed with the Arabic yeh, stored with the Persian one."),
        ("کتاب", "Typed with the Persian kaf (ک), stored with the Arabic one (ك)."),
        ("اسم", "Alef with hamza or madda is plain alef."),
        ("العربيه", "Typed with heh (ه), stored with teh marbuta (ة)."),
        ("۱۲", "Persian digits are ASCII digits."),
        ("٣", "Arabic-Indic digits too."),
        ("ｏｒｄｅｒ", "Fullwidth letters, which some input methods type, are ASCII."),
    ];

    // fzf's --literal: every character as it is. Also used by "A custom matcher".
    private static readonly IBitCommandPaletteMatcher dLiteralMatcher = new BitCommandPaletteFuzzyMatcher { Normalize = false };

    private bool dFoldLiteral;

    private BitCommandPaletteMatch? DFoldMatch(string typed)
    {
        var matcher = dFoldLiteral ? dLiteralMatcher : BitCommandPaletteFuzzyMatcher.Default;
        var query = BitCommandPaletteQuery.Parse(typed);

        return dFoldCommands.Select(command => matcher.Match(command, query))
                            .OfType<BitCommandPaletteMatch>()
                            .MaxBy(match => match.Score);
    }

    private static readonly BitCommandPaletteCommand[] dOperatorCommands =
    [
        BitCommandPaletteCmd.New("Toggle word wrap").Id("d-op-wrap").Group("View"),
        BitCommandPaletteCmd.New("Show minimap").Id("d-op-minimap").Group("View"),
        BitCommandPaletteCmd.New("Show breadcrumbs").Id("d-op-breadcrumbs").Group("View"),
        BitCommandPaletteCmd.New("Wrap selection in a tag").Id("d-op-wrap-tag").Group("Edit"),
        BitCommandPaletteCmd.New("Export project").Id("d-op-export").Group("File"),
        BitCommandPaletteCmd.New("Open project").Id("d-op-open").Group("File"),
        BitCommandPaletteCmd.New("Close project").Id("d-op-close").Group("File"),
        BitCommandPaletteCmd.New("Settings").Id("d-op-settings").Group("Preferences"),
        BitCommandPaletteCmd.New("Set the language").Id("d-op-language").Group("Preferences"),
        BitCommandPaletteCmd.New("Change the theme").Id("d-op-theme").Group("Preferences"),
    ];

    private static readonly (string Query, string Means)[] dOperatorExamples =
    [
        ("wrap word", "Two fuzzy terms, both required, in any order."),
        ("'export", "Exact: the text itself, no gaps."),
        ("^set", "Prefix: a field starts with it."),
        ("project$", "Suffix: a field ends with it."),
        ("show !minimap", "Negation: drops every command that contains the term."),
        ("\"word wrap\"", "A quoted phrase: matched exactly, the space included."),
        ("!\"open project\" project", "A negated phrase: the projects, except Open project."),
        ("^\"set the\"", "An anchored phrase: a field starts with it."),
        ("^change | ^set", "Alternatives: starts with change, or with set."),
    ];

    private BitCommandPaletteQuery dOperatorQuery = BitCommandPaletteQuery.Empty;

    private void DOperatorQueryChanged(string query) => dOperatorQuery = BitCommandPaletteQuery.Parse(query);

    private bool dTypoTolerance = true;

    private static readonly BitCommandPaletteCommand[] dTypoCommands =
    [
        BitCommandPaletteCmd.New("Settings").Id("d-typo-settings"),
        BitCommandPaletteCmd.New("Language").Id("d-typo-language"),
        BitCommandPaletteCmd.New("Privacy").Id("d-typo-privacy"),
        BitCommandPaletteCmd.New("Notifications").Id("d-typo-notifications"),
        BitCommandPaletteCmd.New("Appearance").Id("d-typo-appearance"),
        BitCommandPaletteCmd.New("Keyboard shortcuts").Id("d-typo-shortcuts"),
    ];

    // The default weights; options.Weights holds the app's.
    private static readonly BitCommandPaletteMatchWeights dWeights = new();

    // "color" in a different field of each one.
    private static readonly BitCommandPaletteCommand[] dWeightCommands =
    [
        BitCommandPaletteCmd.New("Color picker").Id("d-weight-title"),
        BitCommandPaletteCmd.New("Brand kit").Id("d-weight-keyword").Keywords("logo", "color"),
        BitCommandPaletteCmd.New("Appearance").Id("d-weight-subtitle").Subtitle("Fonts, color and spacing"),
        BitCommandPaletteCmd.New("Contrast checker").Id("d-weight-group").Group("Color"),
        BitCommandPaletteCmd.New("Design tokens").Id("d-weight-description").Description("Radius, shadow and color values"),
        BitCommandPaletteCmd.New("Export assets").Id("d-weight-tag").Tags("color"),
    ];

    private string dWeightQuery = string.Empty;

    private sealed record DWeightRow(string Command, string Field, double Raw, double Weight);

    private void DWeightQueryChanged(string query) => dWeightQuery = query;

    // What one term scores in each field: the field with the best weighted score counts.
    private List<DWeightRow> DWeightRows()
    {
        var term = dWeightQuery.Trim();
        var rows = new List<DWeightRow>();
        if (term.Length == 0) return rows;

        foreach (var command in dWeightCommands)
        {
            DWeightRow? best = null;
            foreach (var (field, text, weight) in DWeightFields(command))
            {
                var match = BitCommandPaletteFuzzyMatcher.Default.Match(term, text ?? string.Empty);
                if (match.Matched && (best is null || match.Score * weight > best.Raw * best.Weight))
                {
                    best = new(command.Title, field, match.Score, weight);
                }
            }

            if (best is not null)
            {
                rows.Add(best);
            }
        }

        return [.. rows.OrderByDescending(row => row.Raw * row.Weight)];
    }

    private static IEnumerable<(string Field, string? Text, double Weight)> DWeightFields(BitCommandPaletteCommand command)
    {
        yield return ("Title", command.Title, dWeights.Title);
        foreach (var keyword in command.Keywords) yield return ($"Keyword \"{keyword}\"", keyword, dWeights.Keyword);
        yield return ("Subtitle", command.Subtitle, dWeights.Subtitle);
        yield return ("Group", command.Group, dWeights.Group);
        yield return ("Description", command.Description, dWeights.Description);
        foreach (var tag in command.Tags) yield return ($"Tag \"{tag}\"", tag, dWeights.Tag);
    }

    private static readonly BitCommandPaletteCommand[] dRankCommands =
    [
        BitCommandPaletteCmd.New("Reports overview").Id("d-rank-overview").Priority(BitCommandPaletteCommandPriority.High),
        BitCommandPaletteCmd.New("Sales report").Id("d-rank-sales"),
        BitCommandPaletteCmd.New("Weekly report").Id("d-rank-weekly"),
        BitCommandPaletteCmd.New("Report a bug").Id("d-rank-bug").Priority(BitCommandPaletteCommandPriority.Low),
        BitCommandPaletteCmd.New("Archived reports").Id("d-rank-archived").Priority(BitCommandPaletteCommandPriority.Lowest),
    ];

    private string? dRankQuery = "report";
    private double dPriorityWeight = 0.25;  // the default of options.PriorityWeight
    private double dFrecencyWeight = 6;     // the default of options.FrecencyWeight
    private double dRankWeeklyUses = 0;

    private List<BitCommandPaletteMatch> DRankRows()
    {
        // A ranker over options of its own; the palette's ranker uses the app's options.
        var ranker = new BitCommandPaletteRanker(new BitCommandPaletteOptions
        {
            PriorityWeight = dPriorityWeight,
            FrecencyWeight = dFrecencyWeight,
        });

        var query = BitCommandPaletteQuery.Parse(dRankQuery);
        var matches = dRankCommands.Select(command => BitCommandPaletteFuzzyMatcher.Default.Match(command, query))
                                   .OfType<BitCommandPaletteMatch>();
        var now = DateTimeOffset.UtcNow;

        return ranker.Rank(matches, hasQuery: query.Terms.Count > 0, frecencyOf: id => DRankFrecency(id, now));
    }

    // Made-up usage: what the palette's history would have recorded.
    private double DRankFrecency(string commandId, DateTimeOffset now) => commandId switch
    {
        "d-rank-weekly" when dRankWeeklyUses > 0
            => BitCommandPaletteHistory.ComputeFrecency(new((int)dRankWeeklyUses, now.AddHours(-2), false), now),
        "d-rank-bug"
            => BitCommandPaletteHistory.ComputeFrecency(new(3, now.AddDays(-5), false), now),
        _ => 0,
    };

    // A matcher of your own: a field matches when it starts with the term.
    private sealed class DPrefixMatcher : IBitCommandPaletteMatcher
    {
        public BitCommandPaletteFieldMatch Match(string query, string text)
            => text.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                ? new BitCommandPaletteFieldMatch(true, 100 - text.Length, [.. Enumerable.Range(0, query.Length)])
                : BitCommandPaletteFieldMatch.None;
    }

    // dLiteralMatcher, dRanker and dLabCommands are declared by the earlier sections.
    private static readonly IBitCommandPaletteMatcher dIgnoreCaseMatcher = new BitCommandPaletteFuzzyMatcher { IgnoreCase = true };
    private static readonly IBitCommandPaletteMatcher dPrefixMatcher = new DPrefixMatcher();

    private string? dMatcherName = "fuzzy";
    private string? dMatcherQuery = "re";

    private IBitCommandPaletteMatcher DSelectedMatcher() => dMatcherName switch
    {
        "ignore-case" => dIgnoreCaseMatcher,
        "literal" => dLiteralMatcher,
        "substring" => BitCommandPaletteSubstringMatcher.Default,
        "prefix" => dPrefixMatcher,
        _ => BitCommandPaletteFuzzyMatcher.Default,
    };

    private List<BitCommandPaletteMatch> DMatcherRows()
    {
        var matcher = DSelectedMatcher();
        var query = BitCommandPaletteQuery.Parse(dMatcherQuery);
        var matches = new List<BitCommandPaletteMatch>();
        foreach (var command in dLabCommands)
        {
            if (matcher.Match(command, query) is { } match)
            {
                matches.Add(match);
            }
        }

        return dRanker.Rank(matches, hasQuery: query.Terms.Count > 0, frecencyOf: _ => 0);
    }

    private static readonly BitCommandPaletteCommand[] dModeCommands =
    [
        BitCommandPaletteCmd.New("Sort the lines").Id("d-mode-sort").Tags("command")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Sorted the lines"))),
        BitCommandPaletteCmd.New("Toggle word wrap").Id("d-mode-wrap").Tags("command")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Toggled word wrap"))),
        BitCommandPaletteCmd.New("Format the document").Id("d-mode-format").Tags("command")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Formatted the document"))),
        BitCommandPaletteCmd.New("Keyboard shortcuts").Id("d-mode-shortcuts").Group("Help")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Ctrl+K opens the palette"))),
        BitCommandPaletteCmd.New("Search syntax").Id("d-mode-syntax").Group("Help")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("'exact ^prefix suffix$ !not \"a phrase\""))),
        BitCommandPaletteCmd.New("Release notes").Id("d-mode-notes").Group("Help")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Nothing new today"))),
    ];

    private void DRegisterModes()
    {
        registrations.Add(paletteService.Register(dModeCommands));

        registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
        {
            Prefix = ">",
            Name = "Commands",
            Description = "Run an editor command",
            Placeholder = "Run a command…",
            Tags = ["command"],          // only the commands tagged "command"
        }));

        registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
        {
            Prefix = "?",
            Name = "Help",
            Description = "Browse the help topics",
            Groups = ["Help"],           // only the Help group
        }));

        registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
        {
            Prefix = ">>",               // the longest prefix wins over ">"
            Name = "Everything",
            Description = "Every registered command",
            ShowHint = false,            // not listed under Modes
        }));
    }

    private static string DModeScope(BitCommandPaletteCommandMode mode)
        => mode.HideRegistryCommands ? $"only its sources: {string.Join(", ", mode.Sources.Select(s => s.Name))}"
         : mode.Tags is { Count: > 0 } tags ? $"commands tagged {string.Join(", ", tags)}"
         : mode.Groups is { Count: > 0 } groups ? $"the {string.Join(", ", groups)} group"
         : "every command";

    private sealed record DPerson(string Name, string Role, string Email);

    private static readonly DPerson[] dPeople =
    [
        new("Ada Lovelace", "Engineering", "ada@example.com"),
        new("Grace Hopper", "Engineering", "grace@example.com"),
        new("Alan Turing", "Research", "alan@example.com"),
        new("Katherine Johnson", "Data", "katherine@example.com"),
        new("Margaret Hamilton", "Engineering", "margaret@example.com"),
        new("Linus Torvalds", "Platform", "linus@example.com"),
        new("Barbara Liskov", "Architecture", "barbara@example.com"),
        new("Dennis Ritchie", "Platform", "dennis@example.com"),
        new("Hedy Lamarr", "Research", "hedy@example.com"),
        new("Tim Berners-Lee", "Web", "tim@example.com"),
    ];

    private static readonly BitCommandPaletteCommand[] dTeams =
    [
        BitCommandPaletteCmd.New("Platform team").Id("d-team-platform").Subtitle("Builds and runs the platform"),
        BitCommandPaletteCmd.New("Research team").Id("d-team-research").Subtitle("Looks ahead"),
        BitCommandPaletteCmd.New("Data team").Id("d-team-data").Subtitle("Owns the warehouse"),
        BitCommandPaletteCmd.New("Web team").Id("d-team-web").Subtitle("Ships the sites"),
    ];

    private static readonly (string Name, string State)[] dServiceStates =
    [
        ("API", "Operational"),
        ("Web app", "Operational"),
        ("Database", "Degraded performance"),
        ("Search index", "Operational"),
        ("E-mail delivery", "Operational"),
    ];

    private string? dStatusBehaviour = "answers";

    private void DRegisterSources()
    {
        registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
        {
            Prefix = "@",
            Name = "People",
            Description = "Find a teammate or a team",
            Placeholder = "Search people…",
            HideRegistryCommands = true,   // only the sources' rows
            Sources = [new DPeopleSource(this), new BitCommandPaletteStaticSource("Teams", dTeams)],
        }));

        // Asked in the default mode, from 3 characters on.
        registrations.Add(paletteService.RegisterSource(new BitCommandPaletteDelegateSource("Service status", DSearchStatusAsync, minQueryLength: 3)));
    }

    // Re-ranked: returns candidates, the palette scores and highlights them with everything else.
    private sealed class DPeopleSource(BitCommandPaletteDemo page) : IBitCommandPaletteSource
    {
        public string Name => "People";        // the heading of its results
        public int MinQueryLength => 1;        // 0 also asks on the empty query
        public int MaxResults => 20;
        public bool ApplyMatcher => true;      // re-rank with the palette's matcher

        public Task<IReadOnlyList<BitCommandPaletteCommand>> SearchAsync(string query, BitCommandPaletteSourceContext context, CancellationToken cancellationToken)
            => page.DTrackAsync(Name, query, async ct =>
            {
                await Task.Delay(250, ct);     // stands in for the API call; the token cancels it when the query moves on

                return (IReadOnlyList<BitCommandPaletteCommand>)[.. dPeople
                    .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Role.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(p => BitCommandPaletteCmd.New(p.Name)
                        .Id($"d-person-{p.Email}")             // an id of its own: names are not unique
                        .Subtitle(p.Role)
                        .Keywords(p.Email)
                        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($"Opened the profile of {p.Name}")))
                        .Build())];
            }, cancellationToken);
    }

    // Ordered (ApplyMatcher is false): trouble first, kept as one block under the source's name.
    private Task<IReadOnlyList<BitCommandPaletteCommand>> DSearchStatusAsync(string query, CancellationToken cancellationToken)
        => DTrackAsync("Service status", query, async ct =>
        {
            switch (dStatusBehaviour)
            {
                case "throws":
                    await Task.Delay(200, ct);
                    throw new InvalidOperationException("The status service answered 502 Bad Gateway.");
                case "hangs":
                    await Task.Delay(Timeout.Infinite, ct);   // cancelled when SourceTimeoutMs passes
                    break;
            }

            await Task.Delay(200, ct);
            var everything = query.Contains("status", StringComparison.OrdinalIgnoreCase);
            return (IReadOnlyList<BitCommandPaletteCommand>)[.. dServiceStates
                .Where(s => everything || s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.State == "Operational")
                .Select(s => BitCommandPaletteCmd.New(s.Name)
                    .Id($"d-status-{s.Name}")
                    .Subtitle(s.State)
                    .ExcludeFromHistory()
                    .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($"{s.Name}: {s.State}", keepOpen: true)))
                    .Build())];
        }, cancellationToken);

    // The call log: every call that reached a source, and how it ended.
    private sealed class DSourceCall(string source, string query)
    {
        public string Source { get; } = source;
        public string Query { get; } = query;
        public DateTime StartedAt { get; } = DateTime.Now;
        public string Outcome { get; set; } = "Running";
        public int Results { get; set; }
        public TimeSpan Took { get; set; }
    }

    private readonly List<DSourceCall> dSourceCalls = [];

    private async Task<IReadOnlyList<BitCommandPaletteCommand>> DTrackAsync(string source, string query,
        Func<CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>> search, CancellationToken cancellationToken)
    {
        var call = new DSourceCall(source, query);
        lock (dSourceCalls)
        {
            dSourceCalls.Insert(0, call);
            if (dSourceCalls.Count > 12) dSourceCalls.RemoveAt(dSourceCalls.Count - 1);
        }

        _ = InvokeAsync(StateHasChanged);
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            var results = await search(cancellationToken);
            call.Results = results.Count;
            call.Outcome = "Completed";
            return results;
        }
        catch (OperationCanceledException)
        {
            call.Outcome = "Cancelled";
            throw;
        }
        catch
        {
            call.Outcome = "Failed";
            throw;
        }
        finally
        {
            call.Took = System.Diagnostics.Stopwatch.GetElapsedTime(started);
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private DSourceCall[] DSourceCallsSnapshot()
    {
        lock (dSourceCalls) return [.. dSourceCalls];
    }

    private void DClearSourceCalls()
    {
        lock (dSourceCalls) dSourceCalls.Clear();
    }

    private static string DMs(TimeSpan value) => $"{value.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture)} ms";

    private const int DPackagePageSize = 10;

    // Sixty packages: four vendors, fifteen areas.
    private static readonly string[] dPackages =
    [
        .. from vendor in new[] { "Acme", "Contoso", "Fabrikam", "Northwind" }
           from area in new[] { "Core", "Http", "Json", "Logging", "Caching", "Auth", "Data", "Testing", "Blazor", "Grpc", "Metrics", "Search", "Storage", "Mail", "Queue" }
           select $"{vendor}.{area}"
    ];

    private void DRegisterPackages()
    {
        registrations.Add(paletteService.RegisterMode(new BitCommandPaletteCommandMode
        {
            Prefix = "$",
            Name = "Packages",
            Description = $"Browse {dPackages.Length} packages, {DPackagePageSize} at a time",
            Placeholder = "Search packages…",
            HideRegistryCommands = true,
            Sources =
            [
                new BitCommandPaletteDelegateSource("Packages",
                    (query, cursor, context, ct) => DSearchPackagesAsync(query, cursor, ct),
                    minQueryLength: 0,                 // opens on a list
                    maxResults: DPackagePageSize),
            ],
        }));
    }

    // One page of the packages matching the query; logged in the "Async sources" call log.
    private async Task<BitCommandPaletteSourcePage> DSearchPackagesAsync(string query, string? cursor, CancellationToken cancellationToken)
    {
        // The cursor is the offset of the page here; a real API would hand out an opaque token.
        var offset = int.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var from) ? from : 0;
        var hits = dPackages.Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        var commands = await DTrackAsync("Packages", cursor is null ? query : $"{query} (from #{offset + 1})", async ct =>
        {
            await Task.Delay(200, ct); // stands in for the API call

            return (IReadOnlyList<BitCommandPaletteCommand>)[.. hits.Skip(offset).Take(DPackagePageSize).Select(name => BitCommandPaletteCmd.New(name)
                .Id($"d-package-{name}")
                .Subtitle($"{2 + name.Length % 5}.{name.Length % 10}.0")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($"Opened {name}")))
                .Build())];
        }, cancellationToken);

        var next = offset + DPackagePageSize < hits.Count ? (offset + DPackagePageSize).ToString(CultureInfo.InvariantCulture) : null;
        return new BitCommandPaletteSourcePage(commands, next, hits.Count); // the total feeds "Show N more…"
    }

    private string dAssignStatus = "Nobody assigned yet.";
    private BitCommandPaletteCommand[] dPageSourceCommands = default!;

    private void DBuildPageSourceCommands()
    {
        dPageSourceCommands =
        [
            BitCommandPaletteCmd.New("Assign to…")
                .Id("d-page-assign")
                .ChildrenPlaceholder("Search people…")
                .Source("People", DSearchPeopleForPageAsync),

            BitCommandPaletteCmd.New("Request reviews…")
                .Id("d-page-reviewers")
                .ChildrenPlaceholder("Search people, check several, Ctrl+Enter to apply…")
                .MultiSelect()
                .Source("People", DSearchPeopleForPageAsync)
                .Handler(ctx => DSetAssignStatus(ctx.SelectedCommands.Count == 0
                    ? "No reviews requested."
                    : $"Reviews requested from {string.Join(", ", ctx.SelectedCommands.Select(c => c.Title))}.")),

            BitCommandPaletteCmd.New("Browse packages…")
                .Id("d-page-packages")
                .Source("Packages", (query, cursor, ct) => DSearchPackagesAsync(query, cursor, ct), maxResults: DPackagePageSize),
        ];
    }

    private async Task<IReadOnlyList<BitCommandPaletteCommand>> DSearchPeopleForPageAsync(string query, CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken); // stands in for the API call

        return [.. dPeople
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(p => BitCommandPaletteCmd.Action(p.Name, () => DSetAssignStatus($"Assigned to {p.Name}."))
                .Id($"d-assign-{p.Email}")
                .Subtitle(p.Role)
                .Build())];
    }

    // A handler runs inside the palette's event; re-render the page that shows the status.
    private void DSetAssignStatus(string status)
    {
        dAssignStatus = status;
        _ = InvokeAsync(StateHasChanged);
    }

    // dServices is injected in the first section's code-behind; dRanker and DMs are declared by earlier sections.
    private static readonly string[] dPerfQueries = ["c12", "cmd 1234", "command", "synthetic 19", "^command 7", "zzz"];

    private sealed record DPerfMeasurement(string Query, int Matched, TimeSpan Match, TimeSpan Rank, TimeSpan Source);

    private readonly List<DPerfMeasurement> dPerfResults = [];
    private List<BitCommandPaletteCommand> dPerfCommands = [];
    private int dPerfCount = 2000;
    private bool dPerfVirtualize;
    private bool dPerfBusy;
    private IDisposable? dPerfRegistration;
    private TimeSpan? dPerfRegisterTime;

    // A new list each time: the palette takes a new snapshot when Commands gets a new collection.
    private void DBuildPerfCommands()
    {
        dPerfCommands = [.. Enumerable.Range(1, dPerfCount).Select(i => BitCommandPaletteCmd.New($"Command {i}")
            .Id($"d-perf-{i}")
            .Group("Bulk")
            .Subtitle($"Synthetic entry #{i}")
            .Priority(BitCommandPaletteCommandPriority.Lowest)
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($"Ran command {i}", keepOpen: true)))
            .Build())];
        dPerfResults.Clear();
    }

    private void DTogglePerfRegistration()
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (dPerfRegistration is null)
        {
            dPerfRegistration = paletteService.Register(dPerfCommands);
            registrations.Add(dPerfRegistration);
        }
        else
        {
            dPerfRegistration.Dispose();
            registrations.Remove(dPerfRegistration);
            dPerfRegistration = null;
        }

        dPerfRegisterTime = System.Diagnostics.Stopwatch.GetElapsedTime(started);
    }

    private async Task DMeasureAsync()
    {
        dPerfBusy = true;
        dPerfResults.Clear();

        var commands = dPerfCommands;
        var source = new BitCommandPaletteStaticSource("Catalog", commands);
        var context = new BitCommandPaletteSourceContext(null, null, dServices);
        foreach (var text in dPerfQueries)
        {
            // WebAssembly runs on the page's only thread: yielding lets the table fill in as it goes.
            await Task.Delay(1);

            var query = BitCommandPaletteQuery.Parse(text);
            List<TimeSpan> matchTimes = [], rankTimes = [], sourceTimes = [];
            var matched = 0;
            for (var run = 0; run < 5; run++)
            {
                var started = System.Diagnostics.Stopwatch.GetTimestamp();
                var matches = new List<BitCommandPaletteMatch>();
                foreach (var command in commands)
                {
                    if (BitCommandPaletteFuzzyMatcher.Default.Match(command, query) is { } match)
                    {
                        matches.Add(match);
                    }
                }
                matchTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started));

                started = System.Diagnostics.Stopwatch.GetTimestamp();
                dRanker.Rank(matches, hasQuery: true, frecencyOf: _ => 0);
                rankTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started));
                matched = matches.Count;

                started = System.Diagnostics.Stopwatch.GetTimestamp();
                await source.SearchAsync(text, context, CancellationToken.None);
                sourceTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started));
            }

            dPerfResults.Add(new(text, matched, DMedian(matchTimes), DMedian(rankTimes), DMedian(sourceTimes)));
            StateHasChanged();
        }

        dPerfBusy = false;
    }

    private static TimeSpan DMedian(List<TimeSpan> values)
    {
        values.Sort();
        return values[values.Count / 2];
    }

    // ---- Cluster E: shared ------------------------------------------------------------------------------------

    [Inject] private BitCommandPaletteOptions eOptions { get; set; } = default!;
    [Inject] private IJSRuntime eJs { get; set; } = default!;

    /// <summary>Runs an action when disposed, so event subscriptions can join the page's registrations.</summary>
    private sealed class EOnDispose(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }

    private void EInitialize()
    {
        EBuildKeyCommands();
        ERegisterShortcutCommands();
        EBuildOptionCommands();
        EBuildSecurityCommands();

        // The registry table, the platform hints and the sequence status follow the service.
        paletteService.CommandsChanged += ERefresh;
        paletteService.PlatformDetected += ERefresh;
        paletteService.SequencePending += EOnSequencePending;
        registrations.Add(new EOnDispose(() =>
        {
            paletteService.CommandsChanged -= ERefresh;
            paletteService.PlatformDetected -= ERefresh;
            paletteService.SequencePending -= EOnSequencePending;
            eConflictOverride?.Dispose();
            eConflictOverride = null;
        }));
    }

    private void ERefresh() => _ = InvokeAsync(StateHasChanged);

    // ---- Keyboard inside the palette ---------------------------------------------------------------------------

    private bool eLoop = true;
    private bool eVimBindings;
    private bool eDisablePointerSelection;
    private bool eRtl;
    private string eKeyStatus = "Nothing run yet.";
    private BitCommandPaletteCommand[] eKeyCommands = default!;

    private void EBuildKeyCommands()
    {
        eKeyCommands =
        [
            BitCommandPaletteCmd.Action("Open the inbox", () => EKeyRan("Opened the inbox")).Id("e-key-inbox").Group("Mail")
                .Actions(
                    BitCommandPaletteCmd.Action("Mark all as read", () => EKeyRan("Marked all as read")).Id("e-key-inbox-read"),
                    BitCommandPaletteCmd.Action("Archive everything", () => EKeyRan("Archived everything")).Id("e-key-inbox-archive")),
            BitCommandPaletteCmd.Action("Compose a message", () => EKeyRan("Composed a message")).Id("e-key-compose").Group("Mail"),
            BitCommandPaletteCmd.Action("Reply to all", () => EKeyRan("Replied to all")).Id("e-key-reply-all").Group("Mail"),
            BitCommandPaletteCmd.Action("Forward the thread", () => EKeyRan("Forwarded the thread")).Id("e-key-forward").Group("Mail"),
            BitCommandPaletteCmd.Action("Snooze the thread", () => EKeyRan("Snoozed the thread")).Id("e-key-snooze").Group("Mail"),
            BitCommandPaletteCmd.Action("Mute the thread", () => EKeyRan("Muted the thread")).Id("e-key-mute").Group("Mail"),
            BitCommandPaletteCmd.Page("Folders",
                BitCommandPaletteCmd.Action("Drafts", () => EKeyRan("Opened Drafts")).Id("e-key-drafts"),
                BitCommandPaletteCmd.Action("Sent", () => EKeyRan("Opened Sent")).Id("e-key-sent"),
                BitCommandPaletteCmd.Action("Spam", () => EKeyRan("Opened Spam")).Id("e-key-spam"))
                .Id("e-key-folders").Group("Organize"),
            BitCommandPaletteCmd.Page("Label the thread",
                BitCommandPaletteCmd.New("Work").Id("e-key-label-work"),
                BitCommandPaletteCmd.New("Family").Id("e-key-label-family"),
                BitCommandPaletteCmd.New("Travel").Id("e-key-label-travel"))
                .Id("e-key-labels").Group("Organize")
                .MultiSelect()
                .Handler(ctx => EKeyRan(ctx.SelectedCommands.Count == 0
                    ? "Removed every label"
                    : $"Labelled: {string.Join(", ", ctx.SelectedCommands.Select(c => c.Title))}")),
            BitCommandPaletteCmd.Action("Print the thread", () => EKeyRan("Printed the thread (not really)")).Id("e-key-print").Group("Organize"),
            BitCommandPaletteCmd.Action("Block the sender", () => EKeyRan("Blocked the sender")).Id("e-key-block").Group("Organize"),
            BitCommandPaletteCmd.Action("Report spam", () => EKeyRan("Reported spam")).Id("e-key-report").Group("Organize"),
            BitCommandPaletteCmd.Action("Empty the trash", () => EKeyRan("Emptied the trash (not really)")).Id("e-key-trash").Group("Organize")
                .Destructive("Empty the trash? Press Enter again."),
        ];
    }

    private void EKeyRan(string what)
    {
        eKeyStatus = what;
        _ = InvokeAsync(StateHasChanged);
    }

    // ---- Global shortcuts --------------------------------------------------------------------------------------

    private string? eShortcutLast;

    private void ERegisterShortcutCommands()
    {
        registrations.Add(paletteService.Register(
            BitCommandPaletteCmd.Action("New note", () => EShortcutRan("New note"))
                .Id("e-note-new").Group("Notes").Shortcut("Alt+Shift+N"),
            BitCommandPaletteCmd.Action("Go to notes", () => EShortcutRan("Go to notes"))
                .Id("e-note-go").Group("Notes").Shortcut("G N"),
            BitCommandPaletteCmd.Page("Change the note color",
                    BitCommandPaletteCmd.Action("Yellow", () => EShortcutRan("Color: yellow")).Id("e-note-yellow"),
                    BitCommandPaletteCmd.Action("Blue", () => EShortcutRan("Color: blue")).Id("e-note-blue"),
                    BitCommandPaletteCmd.Action("Green", () => EShortcutRan("Color: green")).Id("e-note-green"))
                .Id("e-note-color").Group("Notes").Shortcut("G C"),
            BitCommandPaletteCmd.Action("Discard the draft", () => EShortcutRan("Discard the draft (not really)"))
                .Id("e-note-discard").Group("Notes").Shortcut("Alt+Shift+D")
                .Destructive("Discard the draft?"),
            BitCommandPaletteCmd.Action("Preview the note", () => EShortcutRan("Preview the note"))
                .Id("e-note-preview").Group("Notes").Shortcut("Space", global: false)));

        // The Conflicts section's command: Alt+Q, which its button lets a second command claim.
        registrations.Add(paletteService.Register(
            BitCommandPaletteCmd.Action("Save draft", () => EConflictRan("Save draft"))
                .Id("e-conflict-save").Group("Conflicts").Shortcut("Alt+Q")));
    }

    private void EShortcutRan(string title)
    {
        eShortcutLast = $"{title} at {DateTime.Now:HH:mm:ss}";
        _ = InvokeAsync(StateHasChanged);
    }

    // ---- Sequences in progress ---------------------------------------------------------------------------------

    private BitCommandPaletteKeyboardShortcut? eSequencePending;

    private void EOnSequencePending(BitCommandPaletteKeyboardShortcut? pending)
    {
        eSequencePending = pending;
        _ = InvokeAsync(StateHasChanged);
    }

    // ---- Shortcut conflicts ------------------------------------------------------------------------------------

    private IDisposable? eConflictOverride;
    private string? eConflictLast;

    private string EConflictButtonText => eConflictOverride is null
        ? "Register \"Save and publish\" on Alt+Q"
        : "Dispose \"Save and publish\"";

    private void EToggleConflict()
    {
        if (eConflictOverride is not null)
        {
            eConflictOverride.Dispose();
            eConflictOverride = null;
            return;
        }

        eConflictOverride = paletteService.Register(
            BitCommandPaletteCmd.Action("Save and publish", () => EConflictRan("Save and publish"))
                .Id("e-conflict-publish").Group("Conflicts").Shortcut("Alt+Q"));
    }

    private void EConflictRan(string title)
    {
        eConflictLast = $"{title} at {DateTime.Now:HH:mm:ss}";
        _ = InvokeAsync(StateHasChanged);
    }

    // ---- Shortcut syntax ---------------------------------------------------------------------------------------

    private string? eShortcutText = "Mod+Shift+K";
    private static readonly string[] eShortcutSamples = ["Mod+K", "Ctrl+Shift+P", "G I", "Cmd+Option+Up", "Shift+?", "Alt+Comma", "Ctrl+Shift", "A+B"];

    // ---- Platform-aware hints ----------------------------------------------------------------------------------

    private static readonly BitCommandPaletteKeyboardShortcut eHintChord = "Mod+Shift+K";
    private static readonly BitCommandPaletteKeyboardShortcut eHintSequence = "G S";

    // ---- The toggle shortcut -----------------------------------------------------------------------------------

    private IEnumerable<BitCommandPaletteKeyboardShortcut> EToggleShortcuts
        => eOptions.ToggleShortcut is null
            ? eOptions.AdditionalToggleShortcuts
            : [eOptions.ToggleShortcut, .. eOptions.AdditionalToggleShortcuts];

    // ---- Options -----------------------------------------------------------------------------------------------

    private BitCommandPalette eFinder = default!;
    private string eFinderQuery = "";
    private string? eOptionPicked;
    private BitCommandPaletteCommand[] eOptionCommands = default!;
    private static readonly string[] eFinderQueries = ["history", "shortcut", "source", "^show", ""];

    // A fresh instance: what an app that sets nothing gets.
    private static readonly BitCommandPaletteOptions eDefaultOptions = new();

    private sealed record EOptionInfo(string Name, string Group, Func<BitCommandPaletteOptions, string> Read, string Effect);

    private static readonly EOptionInfo[] eOptionInfos =
    [
        new("ToggleShortcut", "Keyboard", o => o.ToggleShortcut?.ToString() ?? "none", "Opens and closes the palette; null for none. The palette's ToggleShortcut parameter overrides it."),
        new("AdditionalToggleShortcuts", "Keyboard", o => EJoin(o.AdditionalToggleShortcuts), "More shortcuts that open and close the palette."),
        new("SuppressShortcutsInEditableElements", "Keyboard", o => EBool(o.SuppressShortcutsInEditableElements), "Plain keys pressed in an input, a text area or a content-editable element are typing, not shortcuts; chords with a modifier still fire."),
        new("SequenceTimeoutMs", "Keyboard", o => ENumber(o.SequenceTimeoutMs), "How long a sequence (G I) waits for its next chord. Must be positive."),
        new("ShowSequenceHint", "Keyboard", o => EBool(o.ShowSequenceHint), "Shows and announces a sequence in progress."),
        new("Loop", "Keyboard", o => EBool(o.Loop), "Arrowing past the last row comes back to the first."),
        new("VimBindings", "Keyboard", o => EBool(o.VimBindings), "Ctrl+J / Ctrl+K move inside the list; Ctrl+N / Ctrl+P always do."),
        new("Matcher", "Search and ranking", o => o.Matcher.GetType().Name, "Scores and highlights every field: fuzzy by default, BitCommandPaletteSubstringMatcher or your own."),
        new("Weights", "Search and ranking", o => $"{ENumber(o.Weights.Title)} / {ENumber(o.Weights.Keyword)} / {ENumber(o.Weights.Subtitle)} / {ENumber(o.Weights.Group)} / {ENumber(o.Weights.Path)} / {ENumber(o.Weights.Description)} / {ENumber(o.Weights.Tag)}", "Field weights: title, keywords, subtitle, group, path, description, tags."),
        new("MaxResults", "Search and ranking", o => ENumber(o.MaxResults), "Rows kept per query, groups excluded; pinned and recent commands are listed beyond it. Must be positive."),
        new("GroupWhenSearching", "Search and ranking", o => EBool(o.GroupWhenSearching), "Results keep their group headings while searching; false lists one flat ranked list."),
        new("GroupOrder", "Search and ranking", o => EJoin(o.GroupOrder), "Groups listed first, in this order; the rest follow alphabetically."),
        new("PriorityWeight", "Search and ranking", o => ENumber(o.PriorityWeight), "How much a command's priority nudges its rank while searching."),
        new("FrecencyWeight", "Search and ranking", o => ENumber(o.FrecencyWeight), "How much frecency - use, weighted by how recent - nudges it."),
        new("SearchNestedCommands", "Search and ranking", o => EBool(o.SearchNestedCommands), "Nested commands are found from the root page, with their path."),
        new("TypoTolerance", "Search and ranking", o => EBool(o.TypoTolerance), "A query that found nothing is retried allowing a typo per term."),
        new("HighlightMatches", "Search and ranking", o => EBool(o.HighlightMatches), "Matched characters in titles and subtitles are marked."),
        new("DebounceMs", "Modes and sources", o => ENumber(o.DebounceMs), "Quiet time after the last keystroke before async sources are asked. Must not be negative."),
        new("SourceTimeoutMs", "Modes and sources", o => ENumber(o.SourceTimeoutMs), "How long a source may take before it counts as failed and gets a retry row; 0 waits for ever."),
        new("ShowModeHints", "Modes and sources", o => EBool(o.ShowModeHints), "The Modes group listing the registered prefixes on the empty query."),
        new("Commands", "Registered at startup", o => o.Commands.Count == 0 ? "none" : $"{o.Commands.Count} commands", "Commands registered at startup."),
        new("Modes", "Registered at startup", o => o.Modes.Count == 0 ? "none" : string.Join(" ", o.Modes.Select(m => m.Prefix)), "Modes registered at startup."),
        new("Sources", "Registered at startup", o => o.Sources.Count == 0 ? "none" : string.Join(", ", o.Sources.Select(s => s.Name)), "Async sources asked in the default mode."),
        new("EnableHistory", "History", o => EBool(o.EnableHistory), "Records recents, pins and frecency."),
        new("MaxRecents", "History", o => ENumber(o.MaxRecents), "Recent commands listed on the empty query. Must not be negative."),
        new("MaxHistoryEntries", "History", o => ENumber(o.MaxHistoryEntries), "Commands the history keeps at most; pinned ones are never pruned. Must not be negative."),
        new("HistoryStorageKey", "History", o => o.HistoryStorageKey, "The localStorage key of the default history store."),
        new("CloseOnBackdropClick", "Display and behavior", o => EBool(o.CloseOnBackdropClick), "A click on the backdrop closes the palette."),
        new("CloseOnNavigation", "Display and behavior", o => EBool(o.CloseOnNavigation), "The palette closes when the app navigates."),
        new("ResetOnClose", "Display and behavior", o => EBool(o.ResetOnClose), "Closing clears the query and the pages; false reopens where it was left."),
        new("ShowFooter", "Display and behavior", o => EBool(o.ShowFooter), "The hint bar under the list, where toasts show."),
        new("ShowShortcuts", "Display and behavior", o => EBool(o.ShowShortcuts), "Shortcut hints at the end of the rows."),
        new("Virtualize", "Display and behavior", o => EBool(o.Virtualize), "Renders only the visible rows; recommended above about 500."),
        new("RowHeight", "Display and behavior", o => ENumber(o.RowHeight), "A row's height in pixels while virtualized. Must be positive."),
        new("Strings", "Display and behavior", o => o.Strings.GetType().Name, "Every label and announcement of the palette."),
        new("AllowedUrlSchemes", "Security and diagnostics", o => EJoin(o.AllowedUrlSchemes.Order(StringComparer.Ordinal)), "Schemes a command's Href may use; relative links always may."),
        new("RecordCommandIdsInTraces", "Security and diagnostics", o => EBool(o.RecordCommandIdsInTraces), "Whether traces and the log messages about running a command carry its id; metrics never do."),
    ];

    private void EBuildOptionCommands()
    {
        eOptionCommands =
        [
            .. eOptionInfos.Select(o => (BitCommandPaletteCommand)BitCommandPaletteCmd.New(o.Name)
                .Id($"e-option-{o.Name}")
                .Group(o.Group)
                .Subtitle(o.Effect)
                .Value(o.Read(eOptions))
                .Handler(() => EOptionChosen(o))),
        ];
    }

    private void EOptionChosen(EOptionInfo option)
    {
        eOptionPicked = $"{option.Name} = {option.Read(eOptions)} (default: {option.Read(eDefaultOptions)})";
        _ = InvokeAsync(StateHasChanged);
    }

    private static string EBool(bool value) => value ? "true" : "false";

    private static string ENumber(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string EJoin<T>(IEnumerable<T> items)
    {
        var text = string.Join(", ", items);
        return text.Length == 0 ? "none" : text;
    }

    // ---- Options, parameters and BitParams ---------------------------------------------------------------------

    private readonly BitCommandPaletteParams[] eCascadedParams =
    [
        new()
        {
            Placeholder = "Placeholder from BitParams…",
            ShowFooter = false,
            HighlightMatches = false,
            EnableHistory = false,
            VimBindings = true,
        }
    ];

    private static readonly BitCommandPaletteCommand[] eCascadeCommands =
    [
        BitCommandPaletteCmd.New("Banana").Id("e-cascade-banana").Group("Fruits"),
        BitCommandPaletteCmd.New("Mango").Id("e-cascade-mango").Group("Fruits"),
        BitCommandPaletteCmd.New("Orange").Id("e-cascade-orange").Group("Fruits"),
        BitCommandPaletteCmd.New("Pineapple").Id("e-cascade-pineapple").Group("Fruits"),
    ];

    // ---- Security ----------------------------------------------------------------------------------------------

    private const string eUnsafeHref = "javascript:alert('This never runs')";
    private string? eSecurityLast;
    private BitCommandPaletteCommand[] eSecurityCommands = default!;

    private void EBuildSecurityCommands()
    {
        eSecurityCommands =
        [
            BitCommandPaletteCmd.New("Follow a javascript: link").Id("e-security-unsafe-link").Group("Links")
                .Subtitle(eUnsafeHref)
                .Href(eUnsafeHref),
            BitCommandPaletteCmd.New("<img src=x onerror=alert(1)> stays text").Id("e-security-html-title").Group("Text")
                .Handler(() => { }),
            BitCommandPaletteCmd.New("Sam Rivera").Id("e-security-person-1042").Group("People")
                .Subtitle("sam@example.com")
                .ExcludeFromHistory()
                .Handler(() => { }),
        ];
    }

    // ---- History storage ---------------------------------------------------------------------------------------

    private string? eStoredHistory;

    private static readonly BitCommandPaletteCommand[] eHistoryCommands =
    [
        BitCommandPaletteCmd.New("Open the calendar").Id("e-history-calendar").Group("Apps").Handler(() => { }),
        BitCommandPaletteCmd.New("Open the contacts").Id("e-history-contacts").Group("Apps").Handler(() => { }),
        BitCommandPaletteCmd.New("Open the notes").Id("e-history-notes").Group("Apps").Handler(() => { }),
        BitCommandPaletteCmd.New("Open the tasks").Id("e-history-tasks").Group("Apps").Handler(() => { }),
        BitCommandPaletteCmd.New("Open the weather").Id("e-history-weather").Group("Apps").Handler(() => { }),
        BitCommandPaletteCmd.New("Open the photos").Id("e-history-photos").Group("Apps").Handler(() => { }),
    ];

    private async Task EReadStoredHistory()
    {
        try
        {
            var json = await eJs.InvokeAsync<string?>("localStorage.getItem", eOptions.HistoryStorageKey);
            eStoredHistory = string.IsNullOrEmpty(json) ? "(nothing stored yet)" : EIndentJson(json);
        }
        catch (JSException)
        {
            eStoredHistory = "(localStorage is not available in this browser)";
        }
    }

    /// <summary>Indents the stored JSON for reading, without reflection-based serialization (the demo is trimmed).</summary>
    private static string EIndentJson(string json)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            using var stream = new MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream, new() { Indented = true }))
            {
                document.WriteTo(writer);
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (System.Text.Json.JsonException)
        {
            return json;
        }
    }

    // ---------- Cluster F: the files of the template samples ----------

    private sealed record FFileInfo(string Name, string Folder, string Size, string Modified);

    private static readonly FFileInfo[] fRecentFiles =
    [
        new("Program.cs", "src/Server", "4 KB", "Today, 09:12"),
        new("App.razor", "src/Client", "2 KB", "Today, 08:40"),
        new("README.md", "/", "11 KB", "Yesterday"),
        new("appsettings.json", "src/Server", "1 KB", "Monday"),
        new("site.css", "src/Client/wwwroot", "7 KB", "Last week"),
    ];

    private static readonly FFileInfo[] fArchivedFiles =
    [
        new("Startup.cs", "archive/2023", "6 KB", "March 2023"),
        new("web.config", "archive/2023", "2 KB", "March 2023"),
        new("bundleconfig.json", "archive/2022", "1 KB", "June 2022"),
    ];

    private List<BitCommandPaletteCommand> fFiles = [];

    /// <summary>Cluster F's setup; called from OnInitialized.</summary>
    private void FInitialize()
    {
        fFiles =
        [
            .. fRecentFiles.Select((file, i) => FFile(file, "Recent files", i < 2 ? $"Alt+{i + 1}" : null)),
            BitCommandPaletteCmd.New("The archive…").Id("f-archive").Group("Folders").Subtitle("Loads for a second")
                .Children(async (_, ct) =>
                {
                    // Long enough to see the loading state.
                    await Task.Delay(1000, ct);
                    return fArchivedFiles.Select(file => FFile(file, null, null)).ToList();
                }, placeholder: "Search the archive…"),
            BitCommandPaletteCmd.New("An empty folder").Id("f-empty-folder").Group("Folders").Subtitle("Nothing in it")
                .Children(async (_, ct) =>
                {
                    await Task.Delay(300, ct);
                    return [];
                }),
        ];

        fSkinCommands = FBuildSkinCommands();
        fL10nCommands = FBuildL10nCommands();
        fRtlCommands = FBuildRtlCommands();
    }

    private static BitCommandPaletteCommand FFile(FFileInfo file, string? group, string? shortcut)
    {
        var builder = BitCommandPaletteCmd.New(file.Name)
            .Id($"f-file:{file.Folder}/{file.Name}")
            .Group(group)
            .Subtitle(file.Folder)
            .Data(file)
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast($"Opened {file.Name}", keepOpen: true)));

        return shortcut is null ? builder : builder.Shortcut(shortcut, global: false);
    }

    private static string FExtension(BitCommandPaletteCommand command)
    {
        if (command.Children is not null || command.ChildrenProvider is not null) return "dir";

        var dot = command.Title.LastIndexOf('.');
        return dot < 0 ? "txt" : command.Title[(dot + 1)..];
    }

    // ---------- Cluster F: localization ----------

    // What the sample commands say, in one language.
    private sealed record FTexts(
        string General, string DangerZone,
        string OpenSettings, string SettingsOpened,
        string DarkMode,
        string Export, string ChooseFormat, string AsPdf, string ExportedPdf, string AsCsv, string NeedsPro,
        string DeleteFiles, string ConfirmDelete, string Deleted);

    private sealed record FLanguage(string Code, string Name, BitCommandPaletteStrings Strings, FTexts Texts);

    private static readonly FLanguage[] fLanguages =
    [
        new("en", "English", new BitCommandPaletteStrings(), new(
            "General", "Danger zone",
            "Open settings", "Settings opened",
            "Dark mode",
            "Export…", "Choose a format…", "As PDF", "Exported as PDF", "As CSV", "Requires the Pro plan",
            "Delete all files", "Delete every file for good?", "Deleted (not really)")),
        new("de", "Deutsch", new BitCommandPaletteStrings
        {
            Placeholder = "Befehl eingeben oder suchen…",
            PromptPlaceholder = "Wert eingeben und Eingabetaste drücken…",
            NoResults = "Keine Ergebnisse für „{0}“",
            NoResultsHint = "Versuchen Sie eine andere Suche, oder prüfen Sie die Schreibweise.",
            Empty = "Noch nichts anzuzeigen.",
            Loading = "Wird gesucht…",
            Recent = "Zuletzt verwendet",
            Pinned = "Angeheftet",
            Suggestions = "Vorschläge",
            Modes = "Modi",
            Actions = "Aktionen",
            Results = "Ergebnisse",
            Other = "Sonstiges",
            Navigate = "zum Navigieren",
            Select = "zum Auswählen",
            Close = "zum Schließen",
            ClearQuery = "zum Leeren",
            Back = "zurück",
            OpenActions = "für Aktionen",
            Toggle = "zum Umschalten",
            Apply = "zum Anwenden",
            Pin = "Oben anheften",
            Unpin = "Lösen",
            RemoveFromRecents = "Aus „Zuletzt verwendet“ entfernen",
            ClearRecents = "Zuletzt verwendete Befehle löschen",
            ConfirmPrefix = "Bestätigen: ",
            ConfirmHint = "Zum Bestätigen erneut die Eingabetaste drücken, Esc zum Abbrechen",
            SelectedCount = "{0} ausgewählt",
            ResultsAnnouncement = "{0} Ergebnisse verfügbar",
            OneResultAnnouncement = "1 Ergebnis verfügbar",
            NoResultsAnnouncement = "Keine Ergebnisse",
            Disabled = "Nicht verfügbar",
            DialogLabel = "Befehlspalette",
            InputLabel = "Befehle durchsuchen",
            CloseButton = "Schließen",
            BackButton = "Zurück",
            Breadcrumb = "Navigationspfad",
            OpenPalette = "Befehlspalette öffnen",
            TriggerNameSeparator = ", ",
            Search = "Suchen",
            Home = "Start",
            MoreResults = "{0} weitere anzeigen…",
            LoadMore = "Mehr anzeigen…",
            SequencePending = "wartet auf die nächste Taste…",
            CommandFailed = "„{0}“ konnte nicht ausgeführt werden.",
            CommandRefused = "„{0}“ darf nicht ausgeführt werden.",
            CommandUnavailable = "„{0}“ ist nicht mehr verfügbar.",
            LoadFailed = "Diese Liste konnte nicht geladen werden.",
            Retry = "Erneut versuchen",
            ApproximateResults = "Keine genauen Treffer. Ähnliche werden angezeigt.",
            Running = "Wird ausgeführt…",
            SourceFailed = "{0} konnte nicht durchsucht werden",
            On = "Ein",
            Off = "Aus",
            ShortcutThen = "dann",
        }, new(
            "Allgemein", "Gefahrenbereich",
            "Einstellungen öffnen", "Einstellungen geöffnet",
            "Dunkelmodus",
            "Exportieren…", "Format wählen…", "Als PDF", "Als PDF exportiert", "Als CSV", "Erfordert den Pro-Tarif",
            "Alle Dateien löschen", "Wirklich alle Dateien endgültig löschen?", "Gelöscht (nicht wirklich)")),
        new("es", "Español", new BitCommandPaletteStrings
        {
            Placeholder = "Escribe un comando o busca…",
            PromptPlaceholder = "Escribe un valor y pulsa Intro…",
            NoResults = "No hay resultados para «{0}»",
            NoResultsHint = "Prueba con otra búsqueda o revisa la ortografía.",
            Empty = "Todavía no hay nada que mostrar.",
            Loading = "Buscando…",
            Recent = "Recientes",
            Pinned = "Fijados",
            Suggestions = "Sugerencias",
            Modes = "Modos",
            Actions = "Acciones",
            Results = "Resultados",
            Other = "Otros",
            Navigate = "para moverte",
            Select = "para elegir",
            Close = "para cerrar",
            ClearQuery = "para borrar",
            Back = "para volver",
            OpenActions = "para ver acciones",
            Toggle = "para marcar",
            Apply = "para aplicar",
            Pin = "Fijar arriba",
            Unpin = "Dejar de fijar",
            RemoveFromRecents = "Quitar de recientes",
            ClearRecents = "Borrar los comandos recientes",
            ConfirmPrefix = "Confirmar: ",
            ConfirmHint = "Pulsa Intro de nuevo para confirmar, o Esc para cancelar",
            SelectedCount = "{0} seleccionados",
            ResultsAnnouncement = "{0} resultados disponibles",
            OneResultAnnouncement = "1 resultado disponible",
            NoResultsAnnouncement = "Sin resultados",
            Disabled = "No disponible",
            DialogLabel = "Paleta de comandos",
            InputLabel = "Buscar comandos",
            CloseButton = "Cerrar",
            BackButton = "Volver",
            Breadcrumb = "Ruta de navegación",
            OpenPalette = "Abrir la paleta de comandos",
            TriggerNameSeparator = ", ",
            Search = "Buscar",
            Home = "Inicio",
            MoreResults = "Mostrar {0} más…",
            LoadMore = "Mostrar más…",
            SequencePending = "esperando la siguiente tecla…",
            CommandFailed = "No se pudo ejecutar «{0}».",
            CommandRefused = "No puedes ejecutar «{0}».",
            CommandUnavailable = "«{0}» ya no está disponible.",
            LoadFailed = "No se pudo cargar esta lista.",
            Retry = "Reintentar",
            ApproximateResults = "No hay coincidencias exactas. Se muestran las más parecidas.",
            Running = "Trabajando…",
            SourceFailed = "No se pudo buscar en {0}",
            On = "Activado",
            Off = "Desactivado",
            ShortcutThen = "luego",
        }, new(
            "General", "Zona de peligro",
            "Abrir la configuración", "Configuración abierta",
            "Modo oscuro",
            "Exportar…", "Elige un formato…", "Como PDF", "Exportado como PDF", "Como CSV", "Requiere el plan Pro",
            "Eliminar todos los archivos", "¿Eliminar todos los archivos para siempre?", "Eliminados (no de verdad)")),
    ];

    private FLanguage fLanguage = fLanguages[0];
    private List<BitCommandPaletteCommand> fL10nCommands = [];
    private bool fL10nDark;

    private void FSetLanguage(FLanguage language)
    {
        fLanguage = language;
        // A new list: the palette re-reads Commands when it is handed a new collection.
        fL10nCommands = FBuildL10nCommands();
    }

    private List<BitCommandPaletteCommand> FBuildL10nCommands()
    {
        var t = fLanguage.Texts;
        var english = fLanguages[0].Texts;

        return
        [
            // The same id in every language; the English title stays findable as a keyword.
            BitCommandPaletteCmd.New(t.OpenSettings).Id("f-l10n:settings").Group(t.General).Keywords(english.OpenSettings)
                .Shortcut("G S", global: false)
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(t.SettingsOpened, keepOpen: true))),
            BitCommandPaletteCmd.New(t.DarkMode).Id("f-l10n:dark").Group(t.General).Keywords(english.DarkMode)
                .Checked(fL10nDark)
                .Handler(_ =>
                {
                    fL10nDark = !fL10nDark;
                    fL10nCommands = FBuildL10nCommands();
                    StateHasChanged();
                    return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
                }),
            BitCommandPaletteCmd.New(t.Export).Id("f-l10n:export").Group(t.General).Keywords(english.Export)
                .ChildrenPlaceholder(t.ChooseFormat)
                .Children(
                    BitCommandPaletteCmd.New(t.AsPdf).Id("f-l10n:export.pdf").Keywords(english.AsPdf)
                        .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(t.ExportedPdf, keepOpen: true))),
                    BitCommandPaletteCmd.New(t.AsCsv).Id("f-l10n:export.csv").Keywords(english.AsCsv).Disabled(true, t.NeedsPro)),
            BitCommandPaletteCmd.New(t.DeleteFiles).Id("f-l10n:delete").Group(t.DangerZone).Keywords(english.DeleteFiles)
                .Destructive(t.ConfirmDelete)
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast(t.Deleted, keepOpen: true))),
        ];
    }

    // ---------- Cluster F: the commands of the theming samples ----------

    private const string fVariablesStyle =
        "--bit-CommandPalette-width: 760px;" +                       // the modal dialog only
        "--bit-CommandPalette-max-height: 320px;" +
        "--bit-CommandPalette-top: 8vh;" +                           // the modal dialog only
        "--bit-CommandPalette-radius: 1.25rem;" +
        "--bit-CommandPalette-background: color-mix(in srgb, #ea580c 5%, var(--bit-clr-bg-pri));" +
        "--bit-CommandPalette-color: color-mix(in srgb, #9a3412 35%, var(--bit-clr-fg-pri));" +
        "--bit-CommandPalette-border-color: color-mix(in srgb, #ea580c 35%, var(--bit-clr-brd-sec));" +
        "--bit-CommandPalette-shadow: 0 1.5rem 3rem rgba(154, 52, 18, .3);" + // the modal dialog only
        "--bit-CommandPalette-backdrop: rgba(67, 20, 7, .45);" +     // the modal dialog only
        "--bit-CommandPalette-row-height: 3rem;" +
        "--bit-CommandPalette-item-radius: 999px;" +
        "--bit-CommandPalette-active-background: color-mix(in srgb, #ea580c 16%, transparent);" +
        "--bit-CommandPalette-accent: #ea580c;" +
        "--bit-CommandPalette-mark-background: color-mix(in srgb, #ea580c 30%, transparent);" +
        "--bit-CommandPalette-detail-width: 35%;" +
        "--bit-CommandPalette-focus-color: #ea580c;";

    private const string fTriggerStyle =
        "--bit-CommandPaletteTrigger-height: 2.5rem;" +
        "--bit-CommandPaletteTrigger-background: color-mix(in srgb, #ea580c 8%, var(--bit-clr-bg-pri));" +
        "--bit-CommandPaletteTrigger-color: color-mix(in srgb, #9a3412 50%, var(--bit-clr-fg-pri));" +
        "--bit-CommandPaletteTrigger-border-color: #ea580c;" +
        "--bit-CommandPaletteTrigger-radius: 999px;" +
        "--bit-CommandPaletteTrigger-focus-color: #ea580c;";

    private bool fNotifications = true;
    private List<BitCommandPaletteCommand> fSkinCommands = [];

    private List<BitCommandPaletteCommand> FBuildSkinCommands() =>
    [
        BitCommandPaletteCmd.New("Open the inbox").Id("f-skin:inbox").Group("Mail").Subtitle("3 unread").Badge("12")
            .Shortcut("G I", global: false)
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Inbox opened", keepOpen: true))),
        BitCommandPaletteCmd.New("Archive all mail").Id("f-skin:archive").Group("Mail").Destructive("Archive every message?")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Archived (not really)", keepOpen: true))),
        BitCommandPaletteCmd.New("Export the mailbox").Id("f-skin:export").Group("Mail").Disabled(true, "Requires the Pro plan"),
        BitCommandPaletteCmd.New("Notifications").Id("f-skin:notifications").Group("Settings").Checked(fNotifications)
            .Handler(_ =>
            {
                fNotifications = !fNotifications;
                fSkinCommands = FBuildSkinCommands();
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            }),
        BitCommandPaletteCmd.Page("Theme",
                BitCommandPaletteCmd.New("Light").Id("f-skin:theme.light")
                    .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Light (not really)", keepOpen: true))),
                BitCommandPaletteCmd.New("Dark").Id("f-skin:theme.dark")
                    .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("Dark (not really)", keepOpen: true))))
            .Id("f-skin:theme").Group("Settings"),
    ];

    // ---------- Cluster F: ready-made skins ----------

    private sealed record FSkin(string Name, string Style);

    private static readonly FSkin[] fSkins =
    [
        new("Default", ""),
        new("Indigo",
            "--bit-CommandPalette-accent: #4f46e5;" +
            "--bit-CommandPalette-mark-background: rgba(79, 70, 229, .2);" +
            "--bit-CommandPalette-active-background: rgba(79, 70, 229, .1);" +
            "--bit-CommandPalette-radius: 14px;" +
            "--bit-CommandPalette-item-radius: 8px;"),
        new("Teal",
            "--bit-CommandPalette-accent: #0d9488;" +
            "--bit-CommandPalette-background: color-mix(in srgb, #0d9488 4%, var(--bit-clr-bg-pri));" +
            "--bit-CommandPalette-border-color: color-mix(in srgb, #0d9488 30%, var(--bit-clr-brd-sec));" +
            "--bit-CommandPalette-active-background: color-mix(in srgb, #0d9488 14%, transparent);" +
            "--bit-CommandPalette-mark-background: color-mix(in srgb, #0d9488 28%, transparent);"),
        new("Rose",
            "--bit-CommandPalette-accent: #db2777;" +
            "--bit-CommandPalette-background: color-mix(in srgb, #db2777 4%, var(--bit-clr-bg-pri));" +
            "--bit-CommandPalette-border-color: color-mix(in srgb, #db2777 30%, var(--bit-clr-brd-sec));" +
            "--bit-CommandPalette-active-background: color-mix(in srgb, #db2777 12%, transparent);" +
            "--bit-CommandPalette-mark-background: color-mix(in srgb, #db2777 25%, transparent);"),
        new("Flat",
            "--bit-CommandPalette-radius: 0;" +
            "--bit-CommandPalette-item-radius: 0;" +
            "--bit-CommandPalette-shadow: none;" +
            "--bit-CommandPalette-border-color: var(--bit-clr-brd-pri);" +
            "--bit-CommandPalette-active-background: color-mix(in srgb, var(--bit-clr-fg-pri) 10%, transparent);"),
        new("Pill",
            "--bit-CommandPalette-radius: 1.75rem;" +
            "--bit-CommandPalette-item-radius: 999px;" +
            "--bit-CommandPalette-row-height: 3rem;"),
        new("Dense list",
            "--bit-CommandPalette-row-height: 2rem;" +
            "--bit-CommandPalette-max-height: 240px;" +
            "--bit-CommandPalette-item-radius: 4px;" +
            "--bit-CommandPalette-radius: 6px;"),
        new("Terminal",
            "font-family: ui-monospace, SFMono-Regular, Consolas, monospace;" +
            "--bit-CommandPalette-accent: #16a34a;" +
            "--bit-CommandPalette-radius: 4px;" +
            "--bit-CommandPalette-item-radius: 2px;" +
            "--bit-CommandPalette-active-background: color-mix(in srgb, #16a34a 16%, transparent);" +
            "--bit-CommandPalette-mark-background: color-mix(in srgb, #16a34a 30%, transparent);"),
    ];

    private FSkin fSkin = fSkins[1];

    private static readonly (string? Scheme, string Label)[] fSchemes =
    [
        (null, "Follows the page"),
        ("light", "Light"),
        ("dark", "Dark"),
    ];

    private string? fScheme = "dark";

    private readonly BitTheme fDarkTheme = BitThemeFactory.CreateDarkThemeFromSeed(BitAccentColorPresets.Teal);

    private static readonly (string Name, string Scale)[] fDensities =
    [
        ("Compact", "0.8"),
        ("Default", "1"),
        ("Spacious", "1.2"),
    ];

    private string fDensity = "0.8";

    // ---------- Cluster F: the pantry ----------

    private static readonly BitCommandPaletteCommand[] fPantry =
    [
        BitCommandPaletteCmd.New("Apples").Id("f-pantry:apples").Group("Fruit").Subtitle("6 left")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
        BitCommandPaletteCmd.New("Bananas").Id("f-pantry:bananas").Group("Fruit").Subtitle("Ripe")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
        BitCommandPaletteCmd.New("Cherries").Id("f-pantry:cherries").Group("Fruit").Disabled(true, "Out of season"),
        BitCommandPaletteCmd.New("Carrots").Id("f-pantry:carrots").Group("Vegetables").Subtitle("1 kg")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
        BitCommandPaletteCmd.New("Leeks").Id("f-pantry:leeks").Group("Vegetables").Subtitle("3")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
        BitCommandPaletteCmd.New("Throw out the leftovers").Id("f-pantry:leftovers").Group("Vegetables")
            .Destructive("Throw them all out?")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.KeepOpen)),
    ];

    // ---------- Cluster F: Style & Class ----------

    private static readonly BitCommandPaletteCommand[] fColorCommands =
    [
        BitCommandPaletteCmd.New("Violet").Id("f-color:violet").Group("Colors").Badge("Accent")
            .Description("#7c3aed, the accent of these samples."),
        BitCommandPaletteCmd.New("Indigo").Id("f-color:indigo").Group("Colors")
            .Description("#4f46e5, a deep blue-violet."),
        BitCommandPaletteCmd.New("Teal").Id("f-color:teal").Group("Colors")
            .Description("#0d9488, a cool blue-green."),
        BitCommandPaletteCmd.New("Nothing inside").Id("f-color:empty").Group("Pages").Subtitle("Opens an empty page")
            .Children((_, _) => Task.FromResult<IReadOnlyList<BitCommandPaletteCommand>>([])),
    ];

    // ---------- Cluster F: RTL ----------

    // Persian strings that announce the result count in Persian digits.
    private sealed class FPersianStrings : BitCommandPaletteStrings
    {
        public override string ResultsAnnouncementFor(int count) => count switch
        {
            0 => NoResultsAnnouncement,
            1 => OneResultAnnouncement,
            _ => Format(ResultsAnnouncement, FPersianDigits(count)),
        };
    }

    private static string FPersianDigits(int value)
        => string.Concat(value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                              .Select(c => char.IsAsciiDigit(c) ? (char)('۰' + (c - '0')) : c));

    private static readonly FPersianStrings fPersianStrings = new()
    {
        Placeholder = "فرمانی بنویسید یا جست‌وجو کنید…",
        PromptPlaceholder = "مقداری بنویسید و Enter را بزنید…",
        NoResults = "نتیجه‌ای برای «{0}» نیست",
        NoResultsHint = "عبارت دیگری را امتحان کنید، یا املای آن را بررسی کنید.",
        Empty = "هنوز چیزی برای نمایش نیست.",
        Loading = "در حال جست‌وجو…",
        Recent = "اخیر",
        Pinned = "سنجاق‌شده",
        Suggestions = "پیشنهادها",
        Modes = "حالت‌ها",
        Actions = "کارها",
        Results = "نتایج",
        Other = "سایر",
        Navigate = "برای حرکت",
        Select = "برای انتخاب",
        Close = "برای بستن",
        ClearQuery = "برای پاک کردن",
        Back = "برای بازگشت",
        OpenActions = "برای کارها",
        Toggle = "برای تغییر وضعیت",
        Apply = "برای اعمال",
        Pin = "سنجاق کردن به بالا",
        Unpin = "برداشتن سنجاق",
        RemoveFromRecents = "حذف از اخیر",
        ClearRecents = "پاک کردن فرمان‌های اخیر",
        ConfirmPrefix = "تأیید: ",
        ConfirmHint = "برای تأیید دوباره Enter و برای لغو Esc را بزنید",
        SelectedCount = "{0} مورد انتخاب شد",
        ResultsAnnouncement = "{0} نتیجه موجود است",
        OneResultAnnouncement = "یک نتیجه موجود است",
        NoResultsAnnouncement = "نتیجه‌ای نیست",
        Disabled = "در دسترس نیست",
        DialogLabel = "پالت فرمان",
        InputLabel = "جست‌وجوی فرمان‌ها",
        CloseButton = "بستن",
        BackButton = "بازگشت",
        Breadcrumb = "مسیر",
        OpenPalette = "باز کردن پالت فرمان",
        TriggerNameSeparator = "، ",
        Search = "جست‌وجو",
        Home = "خانه",
        MoreResults = "نمایش {0} مورد دیگر…",
        LoadMore = "نمایش بیشتر…",
        SequencePending = "در انتظار کلید بعدی…",
        CommandFailed = "اجرای «{0}» ممکن نشد.",
        CommandRefused = "اجازهٔ اجرای «{0}» را ندارید.",
        CommandUnavailable = "«{0}» دیگر در دسترس نیست.",
        LoadFailed = "این فهرست بارگذاری نشد.",
        Retry = "تلاش دوباره",
        ApproximateResults = "تطابق دقیقی پیدا نشد؛ نزدیک‌ترین‌ها نمایش داده می‌شوند.",
        Running = "در حال انجام…",
        SourceFailed = "جست‌وجو در {0} ممکن نشد",
        On = "روشن",
        Off = "خاموش",
        ShortcutThen = "سپس",
    };

    private List<BitCommandPaletteCommand> fRtlCommands = [];
    private bool fRtlDark;

    private List<BitCommandPaletteCommand> FBuildRtlCommands() =>
    [
        BitCommandPaletteCmd.New("باز کردن تنظیمات").Id("f-rtl:settings").Group("عمومی").Keywords("Open settings")
            .Shortcut("G S", global: false)
            .Actions(BitCommandPaletteCmd.New("کپی کردن پیوند").Id("f-rtl:settings.copy")
                .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("پیوند کپی شد (نه واقعاً)", keepOpen: true))))
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("تنظیمات باز شد", keepOpen: true))),
        BitCommandPaletteCmd.New("حالت تاریک").Id("f-rtl:dark").Group("عمومی").Keywords("Dark mode")
            .Checked(fRtlDark)
            .Handler(_ =>
            {
                fRtlDark = !fRtlDark;
                fRtlCommands = FBuildRtlCommands();
                StateHasChanged();
                return Task.FromResult(BitCommandPaletteCommandResult.KeepOpen);
            }),
        BitCommandPaletteCmd.New("Open in VS Code…").Id("f-rtl:vscode").Group("عمومی").Subtitle("ویرایشگر پیش‌فرض")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("در VS Code باز شد (نه واقعاً)", keepOpen: true))),
        BitCommandPaletteCmd.New("خروجی گرفتن…").Id("f-rtl:export").Group("عمومی").Keywords("Export").Subtitle("۲ قالب")
            .ChildrenPlaceholder("قالبی را انتخاب کنید…")
            .Children(
                BitCommandPaletteCmd.New("به صورت PDF").Id("f-rtl:export.pdf")
                    .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("خروجی PDF گرفته شد", keepOpen: true))),
                BitCommandPaletteCmd.New("به صورت CSV").Id("f-rtl:export.csv").Disabled(true, "به طرح Pro نیاز دارد")),
        BitCommandPaletteCmd.New("حذف همه فایل‌ها").Id("f-rtl:delete").Group("منطقه خطر").Keywords("Delete all files")
            .Destructive("همه فایل‌ها برای همیشه حذف شوند؟")
            .Handler(_ => Task.FromResult(BitCommandPaletteCommandResult.Toast("حذف شد (نه واقعاً)", keepOpen: true))),
    ];
}
