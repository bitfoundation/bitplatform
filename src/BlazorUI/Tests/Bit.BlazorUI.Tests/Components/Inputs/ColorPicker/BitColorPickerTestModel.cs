using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitColorPickerTestModel
{
    // Bound null, a picker shows white without writing it back, so a required color stays missing until one is
    // actually picked.
    [Required]
    public string? Value { get; set; }
}
