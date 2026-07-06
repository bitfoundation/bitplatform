using System;

namespace Bit.Butil;

/// <summary>
/// Focus event payload - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/FocusEvent">FocusEvent</see>.
/// </summary>
public class ButilFocusEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = ["type"];

    /// <summary><c>"focus"</c>, <c>"focusin"</c>, <c>"blur"</c> or <c>"focusout"</c>.</summary>
    public string Type { get; set; } = string.Empty;
}
