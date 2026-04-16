using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

public class BitTagsInputTestModel
{
    [Required]
    [MinLength(1)]
    public ICollection<string>? Tags { get; set; }
}
