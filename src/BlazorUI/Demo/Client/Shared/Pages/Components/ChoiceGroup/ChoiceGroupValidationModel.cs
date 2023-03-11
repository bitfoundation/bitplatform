namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ChoiceGroup;

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = "Pick one")]
    public string Value { get; set; }
}
