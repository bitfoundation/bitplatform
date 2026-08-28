namespace Bit.BlazorUI;

/// <summary>
/// Tells what closed a <see cref="BitPanel"/>, reported to <see cref="BitPanel.OnDismissing"/> through
/// <see cref="BitPanelDismissArgs.Reason"/>.
/// </summary>
public enum BitPanelDismissReason
{
    /// <summary>
    /// The code that opened the panel closed it, through <see cref="BitPanel.Close"/> or
    /// <see cref="BitPanel.Toggle"/>.
    /// </summary>
    Programmatic,

    /// <summary>
    /// The user clicked the overlay that covers the page behind the panel.
    /// </summary>
    Overlay,

    /// <summary>
    /// The user pressed the Escape key while the keyboard was inside the panel.
    /// </summary>
    Escape,

    /// <summary>
    /// The user swiped the panel towards the edge it slid in from.
    /// </summary>
    Swipe,
}
