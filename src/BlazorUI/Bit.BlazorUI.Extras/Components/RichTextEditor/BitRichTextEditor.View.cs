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
        try
        {
            // Only flip the visual state once the browser action has succeeded, so a denied or
            // failed request does not leave the component out of sync with the actual view. The
            // bridge already reports denial through OnClientError, so swallow the interop failure
            // here and keep the previous state.
            await _js.BitRichTextEditorSetFullScreen(_editorRef, next);
        }
        catch (JSException)
        {
            return;
        }
        catch (JSDisconnectedException)
        {
            // The circuit dropped mid-call; leave the state untouched rather than letting the
            // disconnect escape as an unhandled error.
            return;
        }
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
        // Guard on ControlsDisabled (ReadOnly || source view) so block-direction changes can't
        // mutate the hidden editor DOM while source view is active, matching the other commands.
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorSetBlockDirection(_editorRef, dir);
    }

    private string Label(string key, string fallback)
        => Localizer is null ? fallback : (Localizer[key] ?? fallback);
}
