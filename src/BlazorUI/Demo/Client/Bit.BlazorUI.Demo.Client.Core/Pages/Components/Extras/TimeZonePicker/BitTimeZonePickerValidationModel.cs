using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TimeZonePicker;

public class BitTimeZonePickerValidationModel
{
    [Required(ErrorMessage = "Select your time zone.")]
    public string? TimeZoneId { get; set; }
}
