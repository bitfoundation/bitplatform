using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

public class BitFileInputTestModel
{
    // The names of the valid files, joined with commas: a selection holding none of them is no selection at all.
    [Required]
    public string? Value { get; set; }
}
