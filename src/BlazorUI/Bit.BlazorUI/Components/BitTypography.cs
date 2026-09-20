namespace Bit.BlazorUI;

/// <summary>
/// Defines the steps of the theme's typography ramp available in the bit BlazorUI.
/// </summary>
/// <remarks>
/// Each member names one step of the ramp, and the size, the weight, the line height and the tracking of that step
/// are all read from the theme, so a preset re-skins the text of a whole application from one place rather than
/// from every run of text in it.
/// <br />
/// A step also carries the tag it renders on its own, which is what makes the ramp usable without a page having to
/// name one: the six heading steps render their own heading tag, the two subtitles an "h6", the two body steps and
/// <see cref="Inherit"/> a "p", and <see cref="Button"/>, the two captions and <see cref="Overline"/> a "span".
/// That tag is a default and not a decision - where the outline of the document asks for another one,
/// <see cref="BitText.Element"/> names it and the looks stay where they were.
/// </remarks>
public enum BitTypography
{
    /// <summary>
    /// The largest of the six heading steps, for the title of a page. Renders an "h1".
    /// </summary>
    H1,

    /// <summary>
    /// The second heading step. Renders an "h2".
    /// </summary>
    H2,

    /// <summary>
    /// The third heading step. Renders an "h3".
    /// </summary>
    H3,

    /// <summary>
    /// The fourth heading step. Renders an "h4".
    /// </summary>
    H4,

    /// <summary>
    /// The fifth heading step. Renders an "h5".
    /// </summary>
    H5,

    /// <summary>
    /// The smallest of the six heading steps. Renders an "h6".
    /// </summary>
    H6,

    /// <summary>
    /// The larger of the two subtitles, for the line under a heading. Renders an "h6", and is the step a text is
    /// drawn at while none is asked for.
    /// </summary>
    Subtitle1,

    /// <summary>
    /// The smaller of the two subtitles. Renders an "h6".
    /// </summary>
    Subtitle2,

    /// <summary>
    /// The larger of the two body steps, for the copy of a page. Renders a "p".
    /// </summary>
    Body1,

    /// <summary>
    /// The smaller of the two body steps, for the copy that should recede beside it. Renders a "p".
    /// </summary>
    Body2,

    /// <summary>
    /// The step the labels of the interactive controls are drawn at, which the theme also gives a case of its own.
    /// Renders a "span".
    /// </summary>
    Button,

    /// <summary>
    /// The larger of the two captions, for the text that names or annotates something beside it. Renders a "span".
    /// </summary>
    Caption1,

    /// <summary>
    /// The smaller of the two captions. Renders a "span".
    /// </summary>
    Caption2,

    /// <summary>
    /// The smallest step, tracked open and upper cased by the theme, for a label sitting over a section.
    /// Renders a "span".
    /// </summary>
    Overline,

    /// <summary>
    /// Takes every typographic declaration - the family, the size, the weight, the line height, the tracking and
    /// the case - from the element around it rather than from a step of the ramp, which is what a run of text
    /// inside an already styled block needs to keep the look of its surroundings while still taking the colors,
    /// the wrapping and the rest of the parameters of the component. Renders a "p".
    /// </summary>
    Inherit,
}
