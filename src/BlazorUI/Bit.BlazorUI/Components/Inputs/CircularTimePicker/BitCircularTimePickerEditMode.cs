namespace Bit.BlazorUI;

/// <summary>
/// Which parts of the time the <see cref="BitCircularTimePicker"/> lets the user edit.
/// </summary>
public enum BitCircularTimePickerEditMode
{
    /// <summary>
    /// Every part the picker shows can be edited, and settling one moves the dial on to the next.
    /// </summary>
    Normal,

    /// <summary>
    /// Only the minute can be edited; the rest of the current value is kept as it is.
    /// </summary>
    OnlyMinutes,

    /// <summary>
    /// Only the hour can be edited; the rest of the current value is kept as it is.
    /// </summary>
    OnlyHours,

    /// <summary>
    /// Only the second can be edited; the rest of the current value is kept as it is. The picker deals in
    /// seconds in this mode whether or not <see cref="BitCircularTimePicker.ShowSeconds"/> is set.
    /// </summary>
    OnlySeconds
}
