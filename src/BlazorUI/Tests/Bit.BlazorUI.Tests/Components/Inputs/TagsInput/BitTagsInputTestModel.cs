using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitTagsInputTestModel
{
    [Required]
    [MinLength(1)]
    public ICollection<string>? Tags { get; set; }
}
