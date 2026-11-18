using Microsoft.JSInterop;

namespace Bit.Bink;

/// <summary>Receives events from the browser layer.</summary>
internal interface IBinkInteropClient
{
    void OnSamples(ReadOnlySpan<double> data, int count);
    void OnResized(double width, double height, double devicePixelRatio);
    void OnKey(string key, bool ctrl, bool shift, bool alt);
    /// <summary>The element the inline text editor is open for, or null while it is closed.</summary>
    Guid? TextEditorElementId { get; }
    /// <summary>A commit or cancel from the editor that was open for <paramref name="elementId"/> when it was sent.</summary>
    void OnTextEditorKey(string key, string value, Guid elementId);
    void OnRedrawRequested();
    void OnResourcesLost();
    void OnWheel(float panX, float panY, float zoom, float x, float y);
    Task OnPasteAsync(string kind, string? text, IJSStreamReference? data, string? mediaType);
    Task OnDropAsync(string kind, IJSStreamReference data, string? mediaType, float x, float y);
}
