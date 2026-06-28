namespace Bit.BlazorUI;

// Full-screen mode, text direction, and localization.
public partial class BitRichTextEditor
{
    private bool _fullScreen;

    /// <summary>Localized labels/tooltips provider. Null uses built-in English labels.</summary>
    [Parameter] public IBitRichTextEditorLocalizer? Localizer { get; set; }

    private async Task ToggleFullScreen()
    {
        var next = !_fullScreen;
        // Only flip the visual state once the browser action has been issued, so a failed
        // interop call does not leave the component out of sync with the actual view.
        await _js.BitRichTextEditorSetFullScreen(_editorRef, next);
        _fullScreen = next;
        ClassBuilder.Reset();
        StateHasChanged();
    }

    private async Task SetDirectionAsync(string dir)
    {
        if (ReadOnly) return;
        await _js.BitRichTextEditorSetBlockDirection(_editorRef, dir);
    }

    private string Label(string key, string fallback)
        => Localizer is null ? fallback : (Localizer[key] ?? fallback);
}
