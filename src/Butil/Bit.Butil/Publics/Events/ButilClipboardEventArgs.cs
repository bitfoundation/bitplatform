using System;

namespace Bit.Butil;

/// <summary>
/// Clipboard event payload - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/ClipboardEvent">ClipboardEvent</see>.
/// </summary>
public class ButilClipboardEventArgs : EventArgs
{
    // The DataTransfer object isn't directly serializable; events.ts flattens the most
    // common shape for us - the plain-text payload - and leaves richer types to the
    // explicit Clipboard service.
    internal static readonly string[] EventArgsMembers = ["type", "clipboardText"];

    /// <summary><c>"copy"</c>, <c>"cut"</c> or <c>"paste"</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Plain-text contents of the clipboard event, or null when absent.</summary>
    public string? ClipboardText { get; set; }
}
