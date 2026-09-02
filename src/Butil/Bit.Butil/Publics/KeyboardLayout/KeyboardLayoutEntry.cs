namespace Bit.Butil;

/// <summary>
/// One physical key and the character it actually produces on the user's layout - an entry of
/// <see cref="KeyboardLayout.GetLayoutMap"/>.
/// </summary>
public class KeyboardLayoutEntry
{
    /// <summary>
    /// The layout-independent code of the physical key, as it appears in a keyboard event's
    /// <c>code</c>: <c>"KeyW"</c>, <c>"Digit1"</c>, <c>"Semicolon"</c>.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// What that key prints on this layout - <c>"w"</c> on QWERTY, <c>"z"</c> on AZERTY for the same
    /// <c>"KeyW"</c>. This is the string to show the user.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
