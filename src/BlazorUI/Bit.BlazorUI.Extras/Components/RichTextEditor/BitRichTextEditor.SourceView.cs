namespace Bit.BlazorUI;

// HTML source view. While active, the WYSIWYG surface is replaced by a raw-HTML textarea and
// the formatting controls are disabled. On exit the edited HTML is sanitized, validated,
// rendered, and emitted via ValueChanged.
public partial class BitRichTextEditor
{
    private bool _inSourceView;
    private string _sourceText = "";

    private async Task ToggleSourceViewAsync()
    {
        if (ReadOnly) return;
        ClearInlineError();

        if (_inSourceView is false)
        {
            _sourceText = await GetHtmlAsync();
            _inSourceView = true;
            StateHasChanged();
            return;
        }

        // Exiting: validate, sanitize, render.
        if (LooksLikeValidHtml(_sourceText) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-html", "The HTML could not be parsed; fix it before leaving source view."));
            return;
        }

        var sanitized = await _js.BitRichTextEditorSanitizeHtml(_editorRef, _sourceText);

        _inSourceView = false;
        _currentHtml = sanitized;
        await _js.BitRichTextEditorSetHtml(_editorRef, sanitized);
        StateHasChanged();

        await AssignValue(sanitized);
        NotifyEditContextChanged();
        await OnChange.InvokeAsync(sanitized);
    }

    // Lightweight well-formedness check: reject mismatched angle brackets.
    private static bool LooksLikeValidHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return true;
        var open = html.Count(c => c == '<');
        var close = html.Count(c => c == '>');
        return open == close;
    }

    private void OnSourceTextChanged(ChangeEventArgs e)
        => _sourceText = e.Value?.ToString() ?? "";
}
