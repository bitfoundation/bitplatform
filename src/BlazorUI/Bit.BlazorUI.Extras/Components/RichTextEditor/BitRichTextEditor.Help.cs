namespace Bit.BlazorUI;

// The keyboard help: every key the editor answers to, listed in one panel opened by Alt+0 (the chord other editors
// use for it) or by the Help group. The chords are read off the effective shortcut map, so a custom binding shows up
// and a default the host took over does not.
public partial class BitRichTextEditor
{
    private bool _showHelp;
    private ElementReference _helpListRef = default!;

    /// <summary>
    /// The commands a chord can run, in the order the help lists them, each with the localization key and the English
    /// name its toolbar button already uses.
    /// </summary>
    private static readonly (string Command, string Key, string Fallback)[] HelpCommands =
    [
        ("bold", "bold-label", "Bold"),
        ("italic", "italic-label", "Italic"),
        ("underline", "underline-label", "Underline"),
        ("strikeThrough", "strikethrough", "Strikethrough"),
        ("inlineCode", "inline-code-label", "Inline code"),
        ("subscript", "subscript", "Subscript"),
        ("superscript", "superscript", "Superscript"),
        (LinkCommand, "link-label", "Insert or edit link"),
        ("unlink", "remove-link", "Remove link"),
        ("removeFormat", "clear-formatting", "Clear formatting"),
        ("insertUnorderedList", "bullet-list", "Bullet list"),
        ("insertOrderedList", "numbered-list", "Numbered list"),
        ("insertTaskList", "task-list", "Task list"),
        ("indent", "increase-indent", "Increase indent"),
        ("outdent", "decrease-indent", "Decrease indent"),
        ("justifyLeft", "align-left", "Align left"),
        ("justifyCenter", "align-center", "Align center"),
        ("justifyRight", "align-right", "Align right"),
        ("justifyFull", "align-justify", "Justify"),
        ("insertHorizontalRule", "horizontal-rule", "Horizontal rule"),
        ("p", "block-normal", "Normal"),
        ("h1", "heading-1", "Heading 1"),
        ("h2", "heading-2", "Heading 2"),
        ("h3", "heading-3", "Heading 3"),
        ("h4", "heading-4", "Heading 4"),
        ("h5", "heading-5", "Heading 5"),
        ("h6", "heading-6", "Heading 6"),
        ("blockquote", "quote", "Quote"),
        ("pre", "code-block", "Code block"),
        ("undo", "undo-label", "Undo"),
        ("redo", "redo-label", "Redo"),
    ];

    /// <summary>
    /// Reported by the bridge when Alt+0 is pressed in the text: opens the keyboard help, or keeps it open.
    /// </summary>
    [JSInvokable("OnHelpRequested")]
    public async Task _OnHelpRequested()
    {
        if (_showHelp is false) await ToggleHelp();
        StateHasChanged();
    }

    private async Task ToggleHelp()
    {
        _showHelp = !_showHelp;
        if (_showHelp)
        {
            await CloseOtherPanels("help");
            // The list itself takes the focus, so a screen reader starts reading it and the arrow keys scroll it.
            RequestPanelFocus(() => _helpListRef);
        }
        ClearInlineError();
    }

    private async Task CloseHelp()
    {
        if (_showHelp) await ToggleHelp();
        RequestEditorFocus();
    }

    /// <summary>
    /// The rows of the keyboard help: what moves the focus around the editor first, then what edits, then every chord
    /// of the effective shortcut map grouped by the command it runs. A read-only editor lists only the first part,
    /// since nothing else would do anything.
    /// </summary>
    private List<(string Action, IReadOnlyList<string> Keys)> HelpRows()
    {
        var rows = new List<(string, IReadOnlyList<string>)>();

        if (ShowToolbar || ShowQuickToolbar)
        {
            rows.Add((Loc("help-toolbar", "Move to the toolbar"), ["Alt+F10"]));
            rows.Add((Loc("help-toolbar-arrows", "Move between toolbar buttons"), ["←", "→", "Home", "End"]));
        }
        rows.Add((Loc("help-escape", "Close a panel or menu, or leave the toolbar"), ["Escape"]));
        rows.Add((Loc("help-show", "Show these shortcuts"), ["Alt+0"]));

        if (ControlsDisabled) return rows;

        rows.Add((Loc("help-tab", "Nest a list item or move to the next table cell"), ["Tab"]));
        rows.Add((Loc("help-shift-tab", "Lift a list item or move to the previous table cell"), ["Shift+Tab"]));
        rows.Add((Loc("help-task", "Check or uncheck a task"), ["Ctrl+Enter"]));
        rows.Add((Loc("help-plain-paste", "Paste as plain text"), ["Ctrl+Shift+V"]));
        rows.Add((Loc("help-slash", "Open the command menu (on an empty line)"), ["/"]));
        if (OnMentionSearch is not null)
        {
            rows.Add((Loc("help-mention", "Mention someone"), ["@"]));
        }

        var combos = new HashSet<string>(DefaultShortcuts.Keys, StringComparer.OrdinalIgnoreCase);
        if (KeyboardShortcuts is not null) combos.UnionWith(KeyboardShortcuts.Keys);

        var byCommand = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var combo in combos.OrderBy(c => c.Length).ThenBy(c => c, StringComparer.OrdinalIgnoreCase))
        {
            var command = EffectiveShortcutCommand(combo);
            if (command is null || IsKnownCommand(command) is false) continue;
            // Without the link group the link chord is left to the browser (see _OnShortcut).
            if (string.Equals(command, LinkCommand, StringComparison.OrdinalIgnoreCase) && Has(BitRichTextEditorToolbar.Link) is false) continue;

            if (byCommand.TryGetValue(command, out var list) is false) byCommand[command] = list = [];
            list.Add(ToDisplayShortcut(combo));
        }

        foreach (var (command, key, fallback) in HelpCommands)
        {
            if (byCommand.TryGetValue(command, out var keys)) rows.Add((Loc(key, fallback), keys));
        }

        return rows;
    }

    /// <summary>A combo the way the help prints it ("Ctrl+Shift+X"); Ctrl stands for Cmd on macOS, as the help notes.</summary>
    private static string ToDisplayShortcut(string combo)
        => string.Join('+', combo.Split('+', StringSplitOptions.RemoveEmptyEntries).Select(part => part.ToLowerInvariant() switch
        {
            "ctrl" => "Ctrl",
            "shift" => "Shift",
            "alt" => "Alt",
            "meta" => "Meta",
            var other => other.Length == 1 ? other.ToUpperInvariant() : char.ToUpperInvariant(other[0]) + other[1..]
        }));
}
