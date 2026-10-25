using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

public class BitColorPickerTestModel
{
    // Bound null, a picker shows white without writing it back, so a required color stays missing until one is
    // actually picked.
    [Required]
    public string? Value { get; set; }
}
