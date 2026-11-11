using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitTextFieldTestModel
{
    [Required]
    [EmailAddress]
    [StringLength(16)]
    public string? Value { get; set; }
}
