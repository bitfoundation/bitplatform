namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DateRangePicker;

public class FormValidationDateRangePickerModel
{
    [Required(ErrorMessage = "The date range is required.")]
    [CompleteDateRange(ErrorMessage = "Both the start and the end dates are required.")]
    public BitDateRangePickerValue? DateRange { get; set; }
}

/// <summary>
/// Validates that a picked range is complete, since the end date of a BitDateRangePickerValue
/// stays null while only the first date of the range has been selected.
/// </summary>
public class CompleteDateRangeAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not BitDateRangePickerValue range) return true;

        return range.StartDate.HasValue && range.EndDate.HasValue;
    }
}
