namespace Bit.BlazorUI;

/// <summary>An error surfaced by the editor (e.g. invalid URL, failed upload, invalid HTML).</summary>
/// <param name="Code">Stable error code, e.g. "invalid-url".</param>
/// <param name="Message">Human-readable description.</param>
public sealed record BitRichTextEditorError(string Code, string Message);
