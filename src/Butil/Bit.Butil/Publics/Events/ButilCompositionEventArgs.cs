using System;

namespace Bit.Butil;

/// <summary>
/// IME composition event - see <see href="https://developer.mozilla.org/en-US/docs/Web/API/CompositionEvent">CompositionEvent</see>.
/// </summary>
public class ButilCompositionEventArgs : EventArgs
{
    internal static readonly string[] EventArgsMembers = ["type", "data", "locale"];

    /// <summary><c>"compositionstart"</c>, <c>"compositionupdate"</c>, or <c>"compositionend"</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The current composition string.</summary>
    public string? Data { get; set; }

    /// <summary>BCP-47 language tag for the input method, when supplied.</summary>
    public string? Locale { get; set; }
}
