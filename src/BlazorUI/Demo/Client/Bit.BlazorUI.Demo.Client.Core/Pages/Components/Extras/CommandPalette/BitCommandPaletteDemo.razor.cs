namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CommandPalette;

public partial class BitCommandPaletteDemo : IDisposable
{
    [Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

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



    private string? basicLastRun;

    private bool isModalOpen;
    private string? modalLastRun;
    private BitCommandPaletteCommand[] modalCommands = default!;

    private string codeStatus = "Nothing run yet.";
    private bool darkMode;
    private bool compactMode;
    private BitCommandPaletteCommand[] codeCommands = default!;

    private string registeredStatus = "Press Ctrl+Shift+L (Cmd+Shift+L) or G then L anywhere on this page.";
    private int likes;
    private BitCommandPaletteCommand[] pageCommands = default!;

    private string? fruitPicked;
    private IDisposable? fruitMode;

    private string labelsStatus = "No labels set.";
    private BitCommandPaletteCommand[] labelCommands = default!;

    private static readonly string[] fruits = ["Apple", "Apricot", "Banana", "Blueberry", "Cherry", "Grape", "Kiwi", "Lemon", "Mango", "Orange", "Peach", "Pear", "Plum", "Strawberry"];

    private static readonly BitCommandPaletteCommand[] detailCommands =
    [
        BitCommandPaletteCmd.New("Apple").Group("Fruits").Description("A crisp fruit that grows on trees, red, green or yellow."),
        BitCommandPaletteCmd.New("Banana").Group("Fruits").Description("A long, curved fruit with a soft, sweet inside."),
        BitCommandPaletteCmd.New("Cherry").Group("Fruits").Description("A small, round stone fruit, usually dark red."),
    ];

    private static readonly BitCommandPaletteStrings persianStrings = new()
    {
        Placeholder = "فرمانی بنویسید یا جستجو کنید…",
        NoResults = "نتیجه‌ای برای «{0}» یافت نشد",
        DialogLabel = "پالت فرمان",
    };

    protected override void OnInitialized()
    {
        modalCommands =
        [
            BitCommandPaletteCmd.Action("Say hello", () => modalLastRun = "Say hello").Group("Actions").Keywords("greet"),
            BitCommandPaletteCmd.Action("Copy the link", () => modalLastRun = "Copy the link").Group("Actions"),
            BitCommandPaletteCmd.Navigate("Go to getting started", "/getting-started").Group("Navigation"),
        ];

        BuildCodeCommands();

        pageCommands =
        [
            BitCommandPaletteCmd.Action("Like this page", () => { likes++; registeredStatus = $"Liked {likes} time(s)."; })
                .Group("This page").Shortcut("Mod+Shift+L"),
            BitCommandPaletteCmd.Action("Go to the top", () => registeredStatus = "Went to the top.")
                .Group("This page").Shortcut("G L"),
        ];

        // A mode answers what is typed after its prefix: here "#" searches the fruits through an async source, which
        // takes a moment the way a call to an API would.
        fruitMode = paletteService.RegisterMode(new()
        {
            Prefix = "#",
            Name = "Fruits",
            Placeholder = "Search fruits…",
            HideRegistryCommands = true,
            Sources = [new BitCommandPaletteDelegateSource("Fruits", SearchFruits)],
        });

        labelCommands =
        [
            BitCommandPaletteCmd.Page("Set labels",
                BitCommandPaletteCmd.New("Bug").Id("label-bug"),
                BitCommandPaletteCmd.New("Feature").Id("label-feature"),
                BitCommandPaletteCmd.New("Docs").Id("label-docs"),
                BitCommandPaletteCmd.New("Performance").Id("label-performance"))
                .MultiSelect()
                .Handler(ctx => labelsStatus = ctx.SelectedCommands.Count == 0 ? "No labels set." : $"Labels: {string.Join(", ", ctx.SelectedCommands.Select(c => c.Title))}"),
        ];

        base.OnInitialized();
    }

    private async Task<IReadOnlyList<BitCommandPaletteCommand>> SearchFruits(string query, CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken);

        return fruits.Where(f => f.Contains(query, StringComparison.OrdinalIgnoreCase))
                     .Select(f => (BitCommandPaletteCommand)BitCommandPaletteCmd.Action(f, () => fruitPicked = f).Id($"fruit-{f}"))
                     .ToList();
    }

    public void Dispose()
    {
        fruitMode?.Dispose();
    }

    private void BuildCodeCommands()
    {
        codeCommands =
        [
            BitCommandPaletteCmd.Page("Theme",
                BitCommandPaletteCmd.Action("Light", () => SetTheme(false)).Id("theme-light"),
                BitCommandPaletteCmd.Action("Dark", () => SetTheme(true)).Id("theme-dark"))
                .Group("Settings"),
            BitCommandPaletteCmd.New("Compact mode").Group("Settings")
                .Checked(compactMode)
                .Handler(() => { compactMode = !compactMode; codeStatus = $"Compact mode: {compactMode}"; BuildCodeCommands(); }),
            BitCommandPaletteCmd.New("Rename the project").Group("Project")
                .Handler(ctx => Task.FromResult(BitCommandPaletteCommandResult.Prompt("New name", submit =>
                {
                    codeStatus = $"Renamed to {submit.PromptValue}";
                    return Task.FromResult(BitCommandPaletteCommandResult.Toast("Renamed"));
                }))),
            BitCommandPaletteCmd.Action("Delete the project", () => codeStatus = "Deleted").Group("Project")
                .Destructive("Delete the project?"),
        ];
    }

    private void SetTheme(bool dark)
    {
        darkMode = dark;
        codeStatus = $"Theme: {(dark ? "Dark" : "Light")}";
    }
}
