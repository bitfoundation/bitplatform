namespace Bit.BlazorUI;

/// <summary>
/// A step of a <see cref="BitStepper{TItem}"/>.
/// </summary>
public class BitStepperItem
{
    /// <summary>
    /// The aria-label of a step that can be clicked, which replaces its text, description and state as what a screen
    /// reader names it by.
    /// </summary>
    /// <remarks>
    /// A step that cannot be clicked (on a read-only stepper, or ahead on a linear one) is plain content, which an
    /// aria-label is not allowed on, so it is named by its text alone there.
    /// </remarks>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// The custom CSS classes of the step.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// A second line of text under the text of the step.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The icon shown in the indicator of the step in place of its number, using custom CSS classes for external icon
    /// libraries. Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// A step in error shows the stepper's error icon instead, and a completed step the stepper's completed icon.
    /// </remarks>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of the step in place of its number, from the built-in Fluent UI
    /// icons. For external icon libraries, use <see cref="Icon"/> instead.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether the step is disabled: it cannot be gone to, and is drawn dimmed.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Whether the step is in error, which the stepper shows with its error icon and color whatever the position of the
    /// step - a step that failed its validation, say.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// A unique value to use as the key of the step.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The click handler of the step, run when it is clicked and it can be gone to.
    /// </summary>
    public Action<BitStepperItem>? OnClick { get; set; }

    /// <summary>
    /// The custom CSS styles of the step.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The text of the step.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The title (tooltip) of the step.
    /// </summary>
    public string? Title { get; set; }
}
