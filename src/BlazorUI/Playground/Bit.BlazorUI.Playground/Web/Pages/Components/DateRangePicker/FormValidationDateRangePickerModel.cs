using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DateRangePicker;

public class FormValidationDateRangePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
