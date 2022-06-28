using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.DatePicker
{
    public class FormValidationDatePickerModel
    {
        [Required]
        public DateTimeOffset? Date { get; set; }
    }
}
