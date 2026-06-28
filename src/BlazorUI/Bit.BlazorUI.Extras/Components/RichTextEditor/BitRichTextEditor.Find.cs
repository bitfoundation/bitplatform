namespace Bit.BlazorUI;

// Find and replace.
public partial class BitRichTextEditor
{
    private bool _showFind;
    private string _findTerm = "";
    private string _replaceTerm = "";
    private bool _findCaseSensitive;
    private string _findCount = "";

    private async Task ToggleFind()
    {
        _showFind = !_showFind;
        if (_showFind is false)
        {
            _findTerm = "";
            _replaceTerm = "";
            _findCount = "";
            // Await the clear so stale highlight nodes are removed before the panel closes and
            // any JS interop failure surfaces instead of being silently dropped.
            await ClearFindAsync();
        }
        ClearInlineError();
    }

    private async Task ClearFindAsync()
    {
        await _js.BitRichTextEditorClearFind(_editorRef);
    }

    private async Task RunFindAsync()
    {
        if (string.IsNullOrEmpty(_findTerm))
        {
            _findCount = "";
            await _js.BitRichTextEditorClearFind(_editorRef);
            return;
        }
        if (_findTerm.Length > 1000)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-find", "Search term is too long."));
            return;
        }
        var count = await _js.BitRichTextEditorFind(_editorRef, _findTerm, _findCaseSensitive);
        _findCount = count == 0 ? "No matches" : $"{count} match{(count == 1 ? "" : "es")}";
    }

    private async Task ReplaceCurrentAsync()
    {
        // Block replacements while source view is active (ControlsDisabled = ReadOnly || _inSourceView)
        // so the rendered DOM and the raw source text cannot diverge.
        if (ControlsDisabled || string.IsNullOrEmpty(_findTerm)) return;
        if (_findTerm.Length > 1000)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-find", "Search term is too long."));
            return;
        }
        await _js.BitRichTextEditorReplaceCurrent(_editorRef, _findTerm, _replaceTerm, _findCaseSensitive);
        await RunFindAsync();
    }

    private async Task ReplaceAllAsync()
    {
        if (ControlsDisabled || string.IsNullOrEmpty(_findTerm)) return;
        if (_findTerm.Length > 1000)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-find", "Search term is too long."));
            return;
        }
        var n = await _js.BitRichTextEditorReplaceAll(_editorRef, _findTerm, _replaceTerm, _findCaseSensitive);
        _findCount = $"{n} replaced";
    }
}
