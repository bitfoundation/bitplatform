namespace Bit.Butil;

/// <summary>
/// The initial state of a <see cref="TextEditContext"/> - the text the editing surface already shows
/// and where the caret sits in it.
/// </summary>
public class TextEditContextOptions
{
    /// <summary>The text the surface starts with. Empty for a new document.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Where the selection starts, as an offset into <see cref="Text"/>.</summary>
    public int SelectionStart { get; set; }

    /// <summary>Where the selection ends. Equal to <see cref="SelectionStart"/> for a plain caret.</summary>
    public int SelectionEnd { get; set; }
}
