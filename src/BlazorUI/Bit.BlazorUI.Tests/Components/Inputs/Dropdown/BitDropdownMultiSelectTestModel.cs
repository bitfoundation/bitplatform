using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

public class BitDropdownMultiSelectTestModel
{
    [Required]
    [MaxLength(2)]
    [MinLength(2)]
    public IEnumerable<string> Values { get; set; }
}
