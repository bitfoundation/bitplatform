namespace Bit.Butil;

/// <summary>
/// A range the input method wants decorated while a composition is in flight - the payload of
/// <c>textformatupdate</c>.
/// </summary>
/// <remarks>
/// Nothing draws these for you: an <see cref="TextEditContext"/> editor paints its own text, so it
/// has to paint the IME's underlines too. Ignoring them costs the user the visual feedback that
/// tells them which part of the text is still being composed.
/// </remarks>
public class TextEditFormat
{
    /// <summary>Start of the decorated range, as an offset into the context's text.</summary>
    public int RangeStart { get; set; }

    /// <summary>End of the decorated range, exclusive.</summary>
    public int RangeEnd { get; set; }

    /// <summary>Underline style: <c>"none"</c>, <c>"solid"</c>, <c>"dotted"</c>, <c>"dashed"</c>, <c>"wavy"</c>.</summary>
    public string UnderlineStyle { get; set; } = string.Empty;

    /// <summary>Underline thickness: <c>"none"</c>, <c>"thin"</c>, <c>"thick"</c>.</summary>
    public string UnderlineThickness { get; set; } = string.Empty;
}
