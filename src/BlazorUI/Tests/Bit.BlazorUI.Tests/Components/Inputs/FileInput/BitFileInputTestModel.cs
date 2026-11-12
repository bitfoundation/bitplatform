using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitFileInputTestModel
{
    // The names of the valid files, joined with commas: a selection holding none of them is no selection at all.
    [Required]
    public string? Value { get; set; }
}
