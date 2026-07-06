using System;

namespace Bit.Butil;

/// <summary>
/// Input/beforeinput event payload - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/InputEvent">InputEvent</see>.
/// </summary>
public class ButilInputEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = [
        "data", "inputType", "isComposing"];

    /// <summary>The string representing the inserted text. Null for deletions.</summary>
    public string? Data { get; set; }

    /// <summary>e.g. <c>"insertText"</c>, <c>"deleteContentBackward"</c>, <c>"insertFromPaste"</c>.</summary>
    public string InputType { get; set; } = string.Empty;

    /// <summary>True if the event was fired during an IME composition session.</summary>
    public bool IsComposing { get; set; }
}
