using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Web.Pages.Components.DateRangePicker;

public class FormValidationDateRangePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
