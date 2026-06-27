namespace Bit.BlazorUI;

// Full-screen mode, text direction, and localization.
public partial class BitRichTextEditor
{
    private bool _fullScreen;

    /// <summary>Localized labels/tooltips provider. Null uses built-in English labels.</summary>
    [Parameter] public IBitRichTextEditorLocalizer? Localizer { get; set; }

    private async Task ToggleFullScreen()
    {
        _fullScreen = !_fullScreen;
        ClassBuilder.Reset();
        StateHasChanged();
        await _js.BitRichTextEditorSetFullScreen(_editorRef, _fullScreen);
    }

    private async Task SetDirectionAsync(string dir)
    {
        if (ReadOnly) return;
        await _js.BitRichTextEditorSetBlockDirection(_editorRef, dir);
    }

    private string Label(string key, string fallback)
        => Localizer is null ? fallback : (Localizer[key] ?? fallback);
}
