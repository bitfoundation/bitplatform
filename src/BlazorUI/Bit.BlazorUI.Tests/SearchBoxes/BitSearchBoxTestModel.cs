using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.SearchBoxes;

public class BitSearchBoxTestModel
{
    [Required]
    [EmailAddress]
    public string Value { get; set; }
}
