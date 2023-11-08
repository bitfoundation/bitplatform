using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.DatePicker;

public class BitDatePickerTestModel
{
    [Required]
    public DateTimeOffset? Value { get; set; }
}
