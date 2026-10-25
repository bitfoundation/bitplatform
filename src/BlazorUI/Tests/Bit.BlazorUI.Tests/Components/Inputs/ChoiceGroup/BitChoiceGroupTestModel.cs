using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

public class BitChoiceGroupTestModel
{
    [Required]
    public string? Choice { get; set; }
}
