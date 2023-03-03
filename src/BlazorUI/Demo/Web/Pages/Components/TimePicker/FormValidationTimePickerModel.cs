using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Web.Pages.Components.TimePicker;

public class FormValidationTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
