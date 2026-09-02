namespace Bit.Butil;

/// <summary>
/// Where a selection starts and ends inside one element, in characters of that element's text.
/// </summary>
/// <remarks>
/// Offsets count the element's text nodes in order, so element boundaries are not characters and the
/// numbers survive markup changing around the text. That is what makes them safe to save across a
/// re-render and hand back to <see cref="Selection.SelectRange"/>.
/// </remarks>
public class SelectionOffsets
{
    /// <summary>The start offset, in characters.</summary>
    public int Start { get; set; }

    /// <summary>The end offset, in characters. Equal to <see cref="Start"/> for a caret.</summary>
    public int End { get; set; }
}
