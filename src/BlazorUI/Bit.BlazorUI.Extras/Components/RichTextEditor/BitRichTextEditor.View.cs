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

    /// <summary>
    /// Reported by the bridge whenever the browser's full-screen state changes, including exits
    /// triggered outside the component (Escape key, browser UI). Keeps <c>_fullScreen</c> in
    /// sync with the actual view so the toggle button and root class never go stale.
    /// </summary>
    [JSInvokable("OnFullScreenChanged")]
    public void _OnFullScreenChanged(bool isFullScreen)
    {
        if (_fullScreen == isFullScreen) return;
        _fullScreen = isFullScreen;
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
