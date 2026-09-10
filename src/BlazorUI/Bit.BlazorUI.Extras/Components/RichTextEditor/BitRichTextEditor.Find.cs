namespace Bit.BlazorUI;

// Find and replace.
public partial class BitRichTextEditor
{
    // Search terms longer than this are refused: the bridge builds a regular expression from the
    // term and highlights every match, so an unbounded term is only ever a mistake.
    private const int MaxFindTermLength = 1000;

    private bool _showFind;
    private string _findTerm = "";
    private string _replaceTerm = "";
    private bool _findCaseSensitive;
    private bool _findWholeWord;
    private string _findCount = "";
    private int _findTotal;
    private int _findPosition;
    private ElementReference _findInputRef = default!;

    private async Task ToggleFind()
    {
        _showFind = !_showFind;
        if (_showFind)
        {
            CloseOtherPanels("find");
            RequestPanelFocus(() => _findInputRef);
        }
        else
        {
            _findTerm = "";
            _replaceTerm = "";
            ResetFindCount();
            // Await the clear so stale highlight nodes are removed before the panel closes and
            // any JS interop failure surfaces instead of being silently dropped.
            await ClearFindAsync();
        }
        ClearInlineError();
    }

    private void ResetFindCount()
    {
        _findCount = "";
        _findTotal = 0;
        _findPosition = 0;
    }

    private async Task ClearFindAsync()
    {
        await _js.BitRichTextEditorClearFind(_editorRef);
    }

    // Guards every entry point that sends the term to the bridge. Returns false (after surfacing
    // the error) when the term cannot be used.
    private async Task<bool> ValidateFindTermAsync()
    {
        if (_findTerm.Length <= MaxFindTermLength) return true;
        ResetFindCount();
        await RaiseErrorAsync(new BitRichTextEditorError("invalid-find", Label("find-too-long", "Search term is too long.")));
        return false;
    }

    private async Task RunFindAsync()
    {
        if (string.IsNullOrEmpty(_findTerm))
        {
            ResetFindCount();
            ClearInlineError();
            await _js.BitRichTextEditorClearFind(_editorRef);
            return;
        }
        if (await ValidateFindTermAsync() is false) return;

        // The input is valid, so clear any stale "too long" message before running the search.
        ClearInlineError();
        _findTotal = await _js.BitRichTextEditorFind(_editorRef, _findTerm, _findCaseSensitive, _findWholeWord);
        _findPosition = _findTotal > 0 ? 1 : 0;
        UpdateFindCountLabel();
    }

    // Moves the highlighted match forward (+1) or backward (-1), wrapping at the ends, and keeps
    // the readout in sync with the position the bridge reports.
    private async Task StepFindAsync(int delta)
    {
        if (_findTotal == 0)
        {
            await RunFindAsync();
            if (_findTotal == 0) return;
            // The first search already lands on match 1, so a "next" right after it should not skip
            // straight to the second one.
            if (delta > 0) return;
        }
        _findPosition = await _js.BitRichTextEditorFindStep(_editorRef, delta);
        UpdateFindCountLabel();
    }

    // Uses full localized templates per case so translators control word order and pluralization
    // rather than a hard-coded composition.
    private void UpdateFindCountLabel()
    {
        _findCount = _findTotal switch
        {
            0 => Label("no-matches", "No matches"),
            1 => string.Format(Label("match-count", "{0} match"), _findTotal),
            _ => _findPosition > 0
                ? string.Format(Label("match-position", "{0} of {1}"), _findPosition, _findTotal)
                : string.Format(Label("matches-count", "{0} matches"), _findTotal)
        };
    }

    private async Task ReplaceCurrentAsync()
    {
        // Block replacements while source view is active (ControlsDisabled = ReadOnly || _inSourceView)
        // so the rendered DOM and the raw source text cannot diverge.
        if (ControlsDisabled || string.IsNullOrEmpty(_findTerm)) return;
        if (await ValidateFindTermAsync() is false) return;

        ClearInlineError();
        _findTotal = await _js.BitRichTextEditorReplaceCurrent(_editorRef, _findTerm, _replaceTerm, _findCaseSensitive, _findWholeWord);
        _findPosition = _findTotal > 0 ? Math.Min(Math.Max(_findPosition, 1), _findTotal) : 0;
        UpdateFindCountLabel();
    }

    private async Task ReplaceAllAsync()
    {
        if (ControlsDisabled || string.IsNullOrEmpty(_findTerm)) return;
        if (await ValidateFindTermAsync() is false) return;

        // The input is valid, so clear any stale "too long" message before replacing.
        ClearInlineError();
        var n = await _js.BitRichTextEditorReplaceAll(_editorRef, _findTerm, _replaceTerm, _findCaseSensitive, _findWholeWord);
        _findTotal = 0;
        _findPosition = 0;
        _findCount = string.Format(Label("replaced-count", "{0} replaced"), n);
    }

    // Enter runs (or steps to the next match of) the search, Shift+Enter steps backwards, and
    // Escape closes the panel - the shortcuts a find bar is expected to answer to.
    private async Task OnFindKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await StepFindAsync(e.ShiftKey ? -1 : 1);
        else if (e.Key == "Escape") await ToggleFind();
    }
}
