namespace Bit.BlazorUI;

/// <summary>
/// How urgently a live region interrupts a screen reader, which is what the aria-live attribute carries.
/// </summary>
public enum BitPoliteness
{
    /// <summary>
    /// The region is not a live region: nothing in it is announced as it changes (aria-live="off").
    /// </summary>
    Off = 0,

    /// <summary>
    /// The change waits its turn and is announced once the screen reader has finished what it was saying (aria-live="polite").
    /// </summary>
    Polite = 1,

    /// <summary>
    /// The change interrupts the screen reader and is announced right away (aria-live="assertive").
    /// </summary>
    Assertive = 2
}
