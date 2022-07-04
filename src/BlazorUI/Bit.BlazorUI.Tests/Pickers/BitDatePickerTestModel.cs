using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Pickers;

public class BitDatePickerTestModel
{
    [Required]
    public DateTimeOffset? Value { get; set; }
}
