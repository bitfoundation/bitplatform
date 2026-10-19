namespace Bit.Bdnd;

/// <summary>
/// Item types for OS-level (native) drags, mirroring React DnD's HTML5 backend
/// <c>NativeTypes</c> (<c>FILE</c>, <c>URL</c>, <c>TEXT</c>, <c>HTML</c>). A <see cref="BdndDropTarget{TItem}"/>
/// that accepts one of these will react to drags originating outside the application
/// (dragged files, selected text, links, etc.).
/// </summary>
public static class BdndNativeTypes
{
    /// <summary>Dropped files (DataTransfer type <c>Files</c>).</summary>
    public const string File = "__NATIVE_FILE__";

    /// <summary>Dragged links (DataTransfer type <c>text/uri-list</c>).</summary>
    public const string Url = "__NATIVE_URL__";

    /// <summary>Selected text (DataTransfer type <c>text/plain</c>).</summary>
    public const string Text = "__NATIVE_TEXT__";

    /// <summary>Dragged HTML (DataTransfer type <c>text/html</c>).</summary>
    public const string Html = "__NATIVE_HTML__";

    internal static bool IsNative(string type) => type.StartsWith("__NATIVE_", StringComparison.Ordinal);

    /// <summary>Maps a native item type to its <c>DataTransfer</c> type token.</summary>
    internal static string ToDataTransferType(string nativeType) => nativeType switch
    {
        File => "Files",
        Url => "text/uri-list",
        Text => "text/plain",
        Html => "text/html",
        _ => nativeType,
    };
}
