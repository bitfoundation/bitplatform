namespace Bit.BlazorUI;

/// <summary>
/// What made a BitAccordion expand or collapse.
/// </summary>
public enum BitAccordionToggleReason
{
    /// <summary>
    /// The header of the accordion was clicked, or activated by the Enter or the Space key.
    /// </summary>
    Click,

    /// <summary>
    /// The Expand, Collapse or Toggle method of the accordion was called.
    /// </summary>
    Method
}
