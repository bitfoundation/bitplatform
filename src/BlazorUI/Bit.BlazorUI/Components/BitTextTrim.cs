namespace Bit.BlazorUI;

/// <summary>
/// Defines which of the two half-leadings of a run of text is trimmed away in the bit BlazorUI.
/// </summary>
/// <remarks>
/// A line box is taller than the glyphs it draws: the difference between the line height and the font's own metrics
/// is split into a half-leading above the ascenders and one below the descenders, and neither of them belongs to
/// anything a design measures from. Trimming them is what lets a heading be spaced from what surrounds it by the
/// gap that was asked for rather than by that gap plus whatever the line height happened to add.
/// <br />
/// The values map to the CSS "text-box-trim" property, whose edges are read off the font as the cap height above
/// and the alphabetic baseline below. An engine that has not implemented it lays the text out with both leadings
/// intact, exactly as it would have without this, so a page that trims is never broken by one that cannot.
/// </remarks>
public enum BitTextTrim
{
    /// <summary>
    /// Neither half-leading is trimmed, which is what a line box does of its own.
    /// </summary>
    None,

    /// <summary>
    /// The half-leading above the first line is trimmed, so that the top of the box is the cap height of the text.
    /// </summary>
    Start,

    /// <summary>
    /// The half-leading below the last line is trimmed, so that the bottom of the box is the alphabetic baseline.
    /// </summary>
    End,

    /// <summary>
    /// Both half-leadings are trimmed, so that the box is exactly as tall as the glyphs it draws.
    /// </summary>
    Both
}
