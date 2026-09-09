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

    public int MaxLength { get; set; }

    public bool AutoFocus { get; set; }

    /// <summary>
    /// The largest image (in bytes) the paste/drop upload accepts, or 0 for no limit.
    /// </summary>
    public long MaxImageSize { get; set; }

    /// <summary>
    /// A comma separated list of accepted image MIME types (the shape of an input's accept
    /// attribute), or null to accept every image type.
    /// </summary>
    public string? ImageAccept { get; set; }

    /// <summary>
    /// The word shown inside the placeholder that stands in for an image while it uploads.
    /// </summary>
    public string UploadingText { get; set; } = "uploading";

    /// <summary>
    /// Whether the script reports caret moves back to .NET so the toolbar can highlight the
    /// formatting at the caret. Off when nothing on screen would react to it.
    /// </summary>
    public bool ReportSelection { get; set; }

    /// <summary>
    /// Whether Tab inserts an indent instead of moving the focus out of the editor.
    /// </summary>
    public bool TabIndents { get; set; } = true;

    /// <summary>
    /// A signature of every value above, used to detect a config change across renders
    /// without comparing the properties one by one.
    /// </summary>
    public override string ToString() =>
        $"{ImageUpload}|{SyncScroll}|{AutoPair}|{AutoSaveKey}|{ChangeDebounceMs}|{MaxLength}|{AutoFocus}|" +
        $"{ReportSelection}|{TabIndents}|{MaxImageSize}|{ImageAccept}|{UploadingText}";
}
