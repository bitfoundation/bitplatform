namespace Bit.BlazorUI;

// Slash command menu. Markdown shortcuts are handled in the JS bridge; the slash trigger is
// detected there and surfaced here so the menu and command list live in C#.
public partial class BitRichTextEditor
{
    private bool _showSlash;
    private string _slashFilter = "";
    private int _slashIndex;
    private bool _focusSlashPending;
    private ElementReference _slashInputRef = default!;

    private readonly record struct SlashCommand(string Key, string Label, string Command);

    private static readonly SlashCommand[] SlashCommands =
    [
        new("heading-1", "Heading 1", "h1"),
        new("heading-2", "Heading 2", "h2"),
        new("heading-3", "Heading 3", "h3"),
        new("paragraph", "Paragraph", "p"),
        new("bullet-list", "Bulleted list", "insertUnorderedList"),
        new("numbered-list", "Numbered list", "insertOrderedList"),
        new("quote", "Quote", "blockquote"),
        new("code-block", "Code block", "pre"),
    ];

    /// <summary>Called by the bridge when the user types the slash trigger.</summary>
    [JSInvokable("OnSlashTrigger")]
    public void _OnSlashTrigger()
    {
        // Gate on ControlsDisabled (ReadOnly || _inSourceView), matching ApplySlashAsync, so the
        // slash UI is never opened while controls are disabled (e.g. in source-view mode).
        if (ControlsDisabled) return;
        _slashFilter = "";
        _slashIndex = 0;
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
        return SlashCommands.Where(c => Label(c.Key, c.Label).Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    // Resets the highlighted item to the top of the (re)filtered list as the user types so the
    // selection never points past the end of the shrinking result set.
    private void OnSlashFilterInput(ChangeEventArgs e)
    {
        _slashFilter = e.Value?.ToString() ?? "";
        _slashIndex = 0;
    }

    // Keyboard navigation for the slash menu: arrows move the highlight, Enter applies the
    // highlighted command, Escape dismisses the menu. The filter input owns focus while the menu
    // is open, so these keys are handled here and never reach the editor.
    private async Task OnSlashKeyDownAsync(KeyboardEventArgs e)
    {
        if (_showSlash is false) return;

        var items = FilteredSlash().ToList();

        switch (e.Key)
        {
            case "ArrowDown":
                if (items.Count > 0)
                {
                    _slashIndex = (_slashIndex + 1) % items.Count;
                }
                break;

            case "ArrowUp":
                if (items.Count > 0)
                {
                    _slashIndex = (_slashIndex - 1 + items.Count) % items.Count;
                }
                break;

            case "Enter":
                if (items.Count > 0 && _slashIndex >= 0 && _slashIndex < items.Count)
                {
                    await ApplySlashAsync(items[_slashIndex].Command);
                }
                break;

            case "Escape":
                CloseSlash();
                break;
        }
    }

    private void CloseSlash()
    {
        _showSlash = false;
        _slashFilter = "";
        _slashIndex = 0;
    }

    private async Task ApplySlashAsync(string command)
    {
        // Gate on ControlsDisabled (ReadOnly || _inSourceView) so the slash command cannot mutate
        // the WYSIWYG DOM while source view is controlling the visible content.
        if (ControlsDisabled) return;
        _showSlash = false;
        _slashFilter = "";
        _slashIndex = 0;
        await _js.BitRichTextEditorApplySlashCommand(_editorRef, command);
    }

    // Focuses the filter input on the render that follows opening the menu. Called from the main
    // OnAfterRenderAsync so the slash feature owns its own focus handling.
    private async Task FocusSlashIfPendingAsync()
    {
        if (_focusSlashPending is false) return;
        _focusSlashPending = false;
        try
        {
            await _slashInputRef.FocusAsync();
            // Suppress native browser handling of the menu's navigation keys (Arrow/Enter/Escape)
            // on this input while leaving normal typing intact.
            await _js.BitRichTextEditorBindSlashKeys(_slashInputRef);
        }
        catch (JSDisconnectedException) { } // circuit gone; nothing to focus
        catch (JSException) { } // interop unavailable; ignore like ToggleFullScreen does
    }
}
