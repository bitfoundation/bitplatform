using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.TextField;

public class BitTextFieldTestModel
{
    [Required]
    [EmailAddress]
    [StringLength(16)]
    public string Value { get; set; }
}
