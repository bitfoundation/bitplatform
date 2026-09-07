namespace Bit.BlazorUI;

/// <summary>
/// Defines the horizontal alignment of a run of text in the bit BlazorUI.
/// </summary>
/// <remarks>
/// The values are the CSS "text-align" keywords. The alignment moves the lines inside the box the text occupies, so
/// it has nothing to move until that box is wider than its content: an element left inline takes only the width of
/// what it draws, and <see cref="BitText.Block"/> is what gives it a width to align inside.
/// <br />
/// <see cref="Start"/> and <see cref="End"/> are the logical values, and they are the ones to reach for: they
/// follow the direction of the text, so the same markup reads from the leading edge of a left-to-right and of a
/// right-to-left page alike, which <see cref="Left"/> and <see cref="Right"/> deliberately do not.
/// </remarks>
public enum BitTextAlign
{
    /// <summary>
    /// The lines are aligned to the leading edge of the text - the left of a left-to-right page and the right of a
    /// right-to-left one.
    /// </summary>
    Start,

    /// <summary>
    /// The lines are aligned to the trailing edge of the text - the right of a left-to-right page and the left of a
    /// right-to-left one.
    /// </summary>
    End,

    /// <summary>
    /// The lines are aligned to the left whichever way the text around them runs.
    /// </summary>
    Left,

    /// <summary>
    /// The lines are aligned to the right whichever way the text around them runs.
    /// </summary>
    Right,

    /// <summary>
    /// The lines are centered in the box.
    /// </summary>
    Center,

    /// <summary>
    /// The words of every line but the last are spaced so that both edges of the block line up. WCAG advises
    /// against justifying a block of text, since the uneven word spacing it creates is harder to read for people
    /// with dyslexia and other cognitive concerns.
    /// </summary>
    Justify,

    /// <summary>
    /// The same as <see cref="Justify"/>, with the last line of the block justified as well.
    /// </summary>
    JustifyAll,

    /// <summary>
    /// The alignment is the computed one of the parent, with a <see cref="Start"/> or an <see cref="End"/> resolved
    /// against the parent's direction rather than against the element's own.
    /// </summary>
    MatchParent,

    /// <summary>
    /// The alignment is inherited from the parent.
    /// </summary>
    Inherit,

    /// <summary>
    /// The alignment is the initial one of the property, whatever a stylesheet has set it to.
    /// </summary>
    Initial,

    /// <summary>
    /// The alignment rolls back to the one the previous cascade origin - the user's stylesheet, or the browser's -
    /// would have given it.
    /// </summary>
    Revert,

    /// <summary>
    /// The alignment rolls back to the one the previous cascade layer would have given it.
    /// </summary>
    RevertLayer,

    /// <summary>
    /// The alignment is inherited where the property inherits and reset to its initial value where it does not,
    /// which for an inherited property such as this one is the same as <see cref="Inherit"/>.
    /// </summary>
    Unset
}
