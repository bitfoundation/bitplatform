using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.OtpInput;

public class BitOtpInputTestModel
{
    // A code is only ever valid once every one of its characters is in, so the form starts invalid and stays so
    // until the last input is filled.
    [Required(ErrorMessage = "Enter the code")]
    [StringLength(4, MinimumLength = 4, ErrorMessage = "The code is {1} characters long")]
    public string? Code { get; set; }
}
