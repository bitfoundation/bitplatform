namespace Bit.Butil;

/// <summary>
/// One text change from the input method - the payload of the <c>textupdate</c> event, and the only
/// thing an <see cref="TextEditContext"/>-based editor has to act on.
/// </summary>
/// <remarks>
/// Apply it as a splice: replace <c>[UpdateRangeStart, UpdateRangeEnd)</c> of your own buffer with
/// <see cref="Text"/>, then move the caret to <see cref="SelectionStart"/>/<see cref="SelectionEnd"/>.
/// <see cref="Value"/> is the same result computed for you, for an editor that simply re-renders the
/// whole text.
/// </remarks>
public class TextEditContextUpdate
{
    /// <summary>The text being inserted. Empty for a deletion.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Start of the range being replaced, as an offset into the previous text.</summary>
    public int UpdateRangeStart { get; set; }

    /// <summary>End of the range being replaced, exclusive.</summary>
    public int UpdateRangeEnd { get; set; }

    /// <summary>Where the selection should start after the update.</summary>
    public int SelectionStart { get; set; }

    /// <summary>Where the selection should end after the update.</summary>
    public int SelectionEnd { get; set; }

    /// <summary>The context's whole text after the update - the shortcut for an editor that re-renders everything.</summary>
    public string Value { get; set; } = string.Empty;
}
