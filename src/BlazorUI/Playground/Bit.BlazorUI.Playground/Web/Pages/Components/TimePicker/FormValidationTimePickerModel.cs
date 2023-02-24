using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.TimePicker;

public class FormValidationTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
