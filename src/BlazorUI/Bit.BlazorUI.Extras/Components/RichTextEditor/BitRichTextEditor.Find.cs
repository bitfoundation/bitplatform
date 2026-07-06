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
            ClearInlineError();
            await _js.BitRichTextEditorClearFind(_editorRef);
            return;
        }
        if (_findTerm.Length > 1000)
        {
            _findCount = "";
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-find", Label("find-too-long", "Search term is too long.")));
            return;
        }
        // The input is valid, so clear any stale "too long" message before running the search.
        ClearInlineError();
        var count = await _js.BitRichTextEditorFind(_editorRef, _findTerm, _findCaseSensitive);
        // Use full localized templates per case so translators control word order and
        // pluralization rather than the hard-coded "{count} {match/matches}" composition.
        _findCount = count switch
        {
            0 => Label("no-matches", "No matches"),
            1 => string.Format(Label("match-count", "{0} match"), count),
            _ => string.Format(Label("matches-count", "{0} matches"), count)
        };
    }

    private async Task ReplaceCurrentAsync()
    {
        // Block replacements while source view is active (ControlsDisabled = ReadOnly || _inSourceView)
        // so the rendered DOM and the raw source text cannot diverge.
        if (ControlsDisabled || string.IsNullOrEmpty(_findTerm)) return;
        if (_findTerm.Length > 1000)
        {
            _findCount = "";
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-find", Label("find-too-long", "Search term is too long.")));
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
            _findCount = "";
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-find", Label("find-too-long", "Search term is too long.")));
            return;
        }
        // The input is valid, so clear any stale "too long" message before replacing.
        ClearInlineError();
        var n = await _js.BitRichTextEditorReplaceAll(_editorRef, _findTerm, _replaceTerm, _findCaseSensitive);
        _findCount = string.Format(Label("replaced-count", "{0} replaced"), n);
    }
}
