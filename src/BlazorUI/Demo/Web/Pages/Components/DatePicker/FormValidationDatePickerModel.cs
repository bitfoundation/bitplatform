using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Web.Pages.Components.DatePicker;

public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
