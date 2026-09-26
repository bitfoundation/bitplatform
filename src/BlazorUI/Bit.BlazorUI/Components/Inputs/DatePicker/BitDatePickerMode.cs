namespace Bit.BlazorUI;

/// <summary>
/// Determines the selection mode of the BitDatePicker.
/// </summary>
public enum BitDatePickerMode
{
    /// <summary>
    /// Standard date picker mode allowing selection of a specific day.
    /// </summary>
    DatePicker,

    /// <summary>
    /// Month picker mode allowing selection of only month and year.
    /// The day is automatically set to the first selectable day of the selected month.
    /// </summary>
    MonthPicker,

    /// <summary>
    /// Year picker mode allowing selection of only the year.
    /// The day is automatically set to the first selectable day of the selected year.
    /// </summary>
    YearPicker
}

