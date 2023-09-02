namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = "Pick one")]
    public string? Value { get; set; }
}
