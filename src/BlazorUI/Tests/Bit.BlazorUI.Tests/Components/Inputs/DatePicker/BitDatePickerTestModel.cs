using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.DatePicker;

public class BitDatePickerTestModel
{
    [Required]
    public DateTimeOffset? Value { get; set; }
}
