using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Dropdown;

public class BitDropdownMultiSelectTestModel
{
    [Required]
    [MaxLength(2)]
    [MinLength(2)]
    public ICollection<string> Values { get; set; }
}
