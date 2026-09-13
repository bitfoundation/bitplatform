using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

public class BitTimePickerTestModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
