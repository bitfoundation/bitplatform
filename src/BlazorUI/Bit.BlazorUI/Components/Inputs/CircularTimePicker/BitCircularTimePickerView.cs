namespace Bit.BlazorUI;

/// <summary>
/// The part of the time the clock of the <see cref="BitCircularTimePicker"/> is currently editing.
/// </summary>
public enum BitCircularTimePickerView
{
    /// <summary>
    /// The dial selects the hour.
    /// </summary>
    Hour,

    /// <summary>
    /// The dial selects the minute.
    /// </summary>
    Minute,

    /// <summary>
    /// The dial selects the second, which only a picker that shows the seconds ever moves on to.
    /// </summary>
    Second
}
