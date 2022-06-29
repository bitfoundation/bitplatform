using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DatePicker
{
    public class FormValidationDatePickerModel
    {
        [Required]
        public DateTimeOffset? Date { get; set; }
    }
}
