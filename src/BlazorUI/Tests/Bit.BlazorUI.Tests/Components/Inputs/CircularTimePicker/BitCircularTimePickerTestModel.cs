using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.CircularTimePicker;

public class BitCircularTimePickerTestModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
