using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Web.Pages.Components.ChoiceGroup;

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = "Pick one")]
    public string Value { get; set; }
}
