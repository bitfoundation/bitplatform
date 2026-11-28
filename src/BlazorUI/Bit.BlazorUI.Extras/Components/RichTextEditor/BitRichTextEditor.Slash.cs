namespace Bit.BlazorUI;

// Slash command menu. Markdown shortcuts are handled in the JS bridge; the slash trigger is
// detected there and surfaced here so the command list lives in C#. The menu itself - the filter
// box, the highlight, the keys and the ARIA - is the shared _BitEditorMenu.
public partial class BitRichTextEditor
{
    private bool _showSlash;
    private string _slashFilter = "";
    private IReadOnlyList<SlashCommand> _slashItems = SlashCommands;
    private bool _focusSlashPending;
    private _BitEditorMenu<SlashCommand>? _slashMenu;

    private readonly record struct SlashCommand(string Key, string Label, string Command, string Keywords);

    private static readonly SlashCommand[] SlashCommands =
    [
        new("heading-1", "Heading 1", "h1", "h1 title big"),
        new("heading-2", "Heading 2", "h2", "h2 title"),
        new("heading-3", "Heading 3", "h3", "h3 title small"),
        new("paragraph", "Paragraph", "p", "text normal body"),
        new("bullet-list", "Bulleted list", "insertUnorderedList", "ul unordered bullets"),
        new("numbered-list", "Numbered list", "insertOrderedList", "ol ordered numbers"),
        new("task-list", "Task list", "insertTaskList", "todo checklist checkbox"),
        new("quote", "Quote", "blockquote", "blockquote citation"),
        new("code-block", "Code block", "pre", "pre snippet monospace"),
        new("divider", "Divider", "insertHorizontalRule", "hr rule separator line"),
    ];

    // The classes the slash and mention menus are drawn with, which the editor's stylesheet styles.
    private static readonly _BitEditorMenuClasses MenuClasses =
        new("bit-rte-inp", "bit-rte-slash-list", "bit-rte-slash-item", "bit-rte-slash-item-sel", "bit-rte-emoji-empty");

    /// <summary>Called by the bridge when the user types the slash trigger.</summary>
    [JSInvokable("OnSlashTrigger")]
    public void _OnSlashTrigger()
    {
        if (IsDisposing) return;

        // Gate on ControlsDisabled (ReadOnly || _inSourceView), matching ApplySlashAsync, so the
        // slash UI is never opened while controls are disabled (e.g. in source-view mode).
        if (ControlsDisabled) return;
        // The two menus answer different triggers and share the same spot, so only one is ever up.
        CloseMention();
        _slashFilter = "";
        _slashItems = SlashCommands;
        _showSlash = true;
        // Move keyboard focus into the filter input once it renders so typing filters the list
        // (rather than landing in the editor), arrow keys navigate, and Enter applies a command
        // instead of inserting a newline in the contenteditable surface.
        _focusSlashPending = true;
        StateHasChanged();
    }

    private IEnumerable<SlashCommand> FilteredSlash()
    {
        var term = _slashFilter?.Trim();
        if (string.IsNullOrEmpty(term)) return SlashCommands;
        // Match the localized label first, then the English keywords, so a command is still
        // reachable by the word people know it by ("todo", "hr") and not only by its display name.
        return SlashCommands.Where(c => Loc(c.Key, c.Label).Contains(term, StringComparison.OrdinalIgnoreCase)
                                     || c.Keywords.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    // Each keystroke hands the menu a new list, which moves its highlight back to the top so it
    // never points past the end of the shrinking result set.
    private void OnSlashFilterInput(string filter)
    {
        _slashFilter = filter;
        _slashItems = FilteredSlash().ToList();
    }

    private void OnSlashClose()
    {
        CloseSlash();
        RequestEditorFocus();
    }

    private void CloseSlash()
    {
        _showSlash = false;
        _slashFilter = "";
        _slashItems = SlashCommands;
    }

    private async Task ApplySlashAsync(string command)
    {
        // Gate on ControlsDisabled (ReadOnly || _inSourceView) so the slash command cannot mutate
        // the WYSIWYG DOM while source view is controlling the visible content.
        if (ControlsDisabled) return;
        CloseSlash();
        await _js.BitRichTextEditorApplySlashCommand(_editorRef, command);
        RequestEditorFocus();
    }

    // Focuses the filter input on the render that follows opening the menu. Called from the main
    // OnAfterRenderAsync so the slash feature owns its own focus handling.
    private async Task FocusSlashIfPendingAsync()
    {
        if (_focusSlashPending is false) return;
        _focusSlashPending = false;
        if (_slashMenu is not null) await _slashMenu.FocusAsync();
    }
}
