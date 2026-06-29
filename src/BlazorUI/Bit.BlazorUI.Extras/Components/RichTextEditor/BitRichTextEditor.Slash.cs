namespace Bit.BlazorUI;

// Slash command menu. Markdown shortcuts are handled in the JS bridge; the slash trigger is
// detected there and surfaced here so the menu and command list live in C#.
public partial class BitRichTextEditor
{
    private bool _showSlash;
    private string _slashFilter = "";

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
        if (ReadOnly) return;
        _slashFilter = "";
        _showSlash = true;
        StateHasChanged();
    }

    private IEnumerable<SlashCommand> FilteredSlash()
    {
        var term = _slashFilter?.Trim();
        if (string.IsNullOrEmpty(term)) return SlashCommands;
        return SlashCommands.Where(c => Label(c.Key, c.Label).Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private void CloseSlash()
    {
        _showSlash = false;
        _slashFilter = "";
    }

    private async Task ApplySlashAsync(string command)
    {
        // Gate on ControlsDisabled (ReadOnly || _inSourceView) so the slash command cannot mutate
        // the WYSIWYG DOM while source view is controlling the visible content.
        if (ControlsDisabled) return;
        _showSlash = false;
        _slashFilter = "";
        await _js.BitRichTextEditorApplySlashCommand(_editorRef, command);
    }
}
