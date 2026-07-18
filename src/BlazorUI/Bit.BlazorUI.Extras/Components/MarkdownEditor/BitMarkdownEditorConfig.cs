namespace Bit.BlazorUI;

/// <summary>
/// The initialization config passed to the MarkdownEditor JS interop script.
/// This is a named type (not an anonymous one) so that its properties are
/// serialized by name and it stays trim/AOT-safe: anonymous types lose their
/// constructor parameter names when trimmed, which breaks System.Text.Json.
/// </summary>
internal sealed class BitMarkdownEditorConfig
{
    public bool ImageUpload { get; set; }

    public bool SyncScroll { get; set; }

    public bool AutoPair { get; set; }

    public string? AutoSaveKey { get; set; }

    public int ChangeDebounceMs { get; set; }
}
