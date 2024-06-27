using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.SearchBox;

public class BitSearchBoxTestModel
{
    [Required]
    [EmailAddress]
    public string Value { get; set; }
}
