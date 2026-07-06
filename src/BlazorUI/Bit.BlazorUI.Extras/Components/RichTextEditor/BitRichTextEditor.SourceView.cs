namespace Bit.BlazorUI;

// HTML source view. While active, the WYSIWYG surface is replaced by a raw-HTML textarea and
// the formatting controls are disabled. On exit the edited HTML is sanitized, validated,
// rendered, and emitted via ValueChanged.
public partial class BitRichTextEditor
{
    private bool _inSourceView;
    private string _sourceText = "";
    private ElementReference _sourceRef = default!;

    private async Task ToggleSourceViewAsync()
    {
        // ReadOnly blocks *entering* source view, but exiting must stay possible: if the host
        // flips ReadOnly to true while source view is open, the editor would otherwise be
        // trapped there with no way back to the rendered view.
        if (ReadOnly && _inSourceView is false) return;
        ClearInlineError();

        if (_inSourceView is false)
        {
            _sourceText = await GetHtmlAsync();
            _inSourceView = true;
            StateHasChanged();
            return;
        }

        // If ReadOnly was flipped on while source view was open, leaving must not sanitize,
        // assign, or emit the edited source: that would mutate content the read-only contract
        // forbids. Just exit back to the rendered (unchanged) view.
        if (ReadOnly)
        {
            _inSourceView = false;
            StateHasChanged();
            return;
        }

        // Exiting: validate, sanitize, render.
        if (await _js.BitRichTextEditorValidateHtml(_editorRef, _sourceText) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-html",
                Label("invalid-html", "The HTML could not be parsed; fix it before leaving source view.")));
            return;
        }

        var sanitized = await _js.BitRichTextEditorSanitizeHtml(_editorRef, _sourceText);

        // If the sanitized source is identical to what the editor already holds, there is no
        // effective content change: just leave source view without re-rendering or re-notifying.
        if (sanitized == _currentHtml)
        {
            _inSourceView = false;
            StateHasChanged();
            return;
        }

        // Push the sanitized HTML to the editor DOM first; only mutate source-view/cached state
        // once the interop bridge succeeds, so a failing bridge call leaves the editor and bound
        // value consistent (still in source view) rather than half-committed.
        await _js.BitRichTextEditorSetHtml(_editorRef, sanitized);
        _inSourceView = false;
        _currentHtml = sanitized;
        StateHasChanged();

        await AssignValue(sanitized);
        NotifyEditContextChanged();
        await OnChange.InvokeAsync(sanitized);
    }

    private void OnSourceTextChanged(ChangeEventArgs e)
        => _sourceText = e.Value?.ToString() ?? "";
}
