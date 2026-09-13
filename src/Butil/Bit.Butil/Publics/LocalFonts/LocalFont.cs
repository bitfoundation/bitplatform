namespace Bit.Butil;

/// <summary>
/// One font face installed on the user's machine.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/FontData">FontData</see>
/// </summary>
public class LocalFont
{
    /// <summary>
    /// The PostScript name - the face's unique identifier, and what
    /// <see cref="LocalFonts.GetData"/> takes.
    /// </summary>
    public string PostscriptName { get; set; } = string.Empty;

    /// <summary>The full human-readable name, e.g. <c>"Helvetica Neue Bold Italic"</c>.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>The family the face belongs to, e.g. <c>"Helvetica Neue"</c> - what to group a picker by.</summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>The style within the family, e.g. <c>"Bold Italic"</c>.</summary>
    public string Style { get; set; } = string.Empty;
}
