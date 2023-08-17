namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Calendar;

public class FormValidationCalendarModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
