namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Calendar;

public class BitCalendarValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
