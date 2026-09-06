namespace Bit.BlazorUI;

/// <summary>
/// Defines how the lines of a run of text are broken in the bit BlazorUI.
/// </summary>
/// <remarks>
/// The values are the CSS "text-wrap" keywords. Every one of them but <see cref="Wrap"/> and <see cref="NoWrap"/>
/// asks the browser for a better set of break points rather than for a different set of rules, so an engine that
/// does not implement one simply lays the text out the way it would have anyway.
/// </remarks>
public enum BitTextWrap
{
    /// <summary>
    /// The text is broken into lines the usual way.
    /// </summary>
    Wrap,

    /// <summary>
    /// The text is not broken into lines at all and overflows its container instead.
    /// </summary>
    NoWrap,

    /// <summary>
    /// The lines are balanced so that they come out of a similar length, which is what a heading of two or three
    /// lines needs to keep a single word from being left alone on the last one. The engines only balance a short
    /// block - six lines in Chromium, ten in Firefox - and lay a longer one out normally.
    /// </summary>
    Balance,

    /// <summary>
    /// The break points are chosen by the slower algorithm that avoids leaving a short last line, and in WebKit
    /// also evens out the ragged edge and limits the hyphenation. This is the one for body copy.
    /// </summary>
    Pretty,

    /// <summary>
    /// The lines already laid out keep their break points while the text after them is edited, which is what an
    /// editable surface needs to keep the whole paragraph from re-flowing under the caret on every keystroke.
    /// </summary>
    Stable
}
