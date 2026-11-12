using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitDropdownMultiSelectTestModel
{
    [Required]
    [MaxLength(2)]
    [MinLength(2)]
    public IEnumerable<string?>? Values { get; set; }
}
