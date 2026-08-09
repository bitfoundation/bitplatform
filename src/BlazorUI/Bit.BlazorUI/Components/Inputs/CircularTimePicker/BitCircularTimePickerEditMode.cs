namespace Bit.BlazorUI;

/// <summary>
/// Which parts of the time the <see cref="BitCircularTimePicker"/> lets the user edit.
/// </summary>
public enum BitCircularTimePickerEditMode
{
    /// <summary>
    /// Both the hour and the minute can be edited, and picking an hour moves the dial on to the minutes.
    /// </summary>
    Normal,

    /// <summary>
    /// Only the minute can be edited; the hour of the current value is kept as it is.
    /// </summary>
    OnlyMinutes,

    /// <summary>
    /// Only the hour can be edited; the minute of the current value is kept as it is.
    /// </summary>
    OnlyHours
}
