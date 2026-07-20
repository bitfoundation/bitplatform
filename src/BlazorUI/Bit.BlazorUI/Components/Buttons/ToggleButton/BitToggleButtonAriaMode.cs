namespace Bit.BlazorUI;

/// <summary>
/// Determines which ARIA state attribute the toggle button exposes to assistive technologies.
/// </summary>
public enum BitToggleButtonAriaMode
{
    /// <summary>
    /// Renders <c>aria-pressed</c>, unless the accessible name of the toggle button changes between the
    /// checked and unchecked states, in which case no state attribute is rendered.
    /// <br />
    /// Changing the accessible name and the pressed state at the same time makes the toggle button
    /// ambiguous to screen reader users, so the changing name is treated as the single source of truth.
    /// </summary>
    Auto,

    /// <summary>
    /// Always renders <c>aria-pressed</c>, even when the accessible name changes between the two states.
    /// </summary>
    Pressed,

    /// <summary>
    /// Renders <c>role="switch"</c> along with <c>aria-checked</c> instead of <c>aria-pressed</c>.
    /// <br />
    /// Use it when an on/off reading fits the toggle button better than a pressed/not pressed one.
    /// </summary>
    Switch,

    /// <summary>
    /// Renders no state attribute at all.
    /// <br />
    /// Use it when the content of the toggle button already conveys the state, like a play/pause button.
    /// </summary>
    None
}
