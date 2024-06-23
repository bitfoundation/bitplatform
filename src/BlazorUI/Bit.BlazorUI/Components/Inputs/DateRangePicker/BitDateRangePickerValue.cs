﻿namespace Bit.BlazorUI;

public class BitDateRangePickerValue
{
    /// <summary>
    /// Indicates the beginning of the date range.
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Indicates the end of the date range.
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}
