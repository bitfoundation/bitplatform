using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.RadioButtonGroup;

public class FormValidationModel
{
    [Required]
    public string Option { get; set; }
}
