using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitChoiceGroupTestModel
{
    [Required]
    public string? Choice { get; set; }
}
