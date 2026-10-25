using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Calendar;

public class BitCalendarTestModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
