using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

public class BitDateRangePickerTestModel
{
    [Required]
    public BitDateRangePickerValue? Value { get; set; }
}
