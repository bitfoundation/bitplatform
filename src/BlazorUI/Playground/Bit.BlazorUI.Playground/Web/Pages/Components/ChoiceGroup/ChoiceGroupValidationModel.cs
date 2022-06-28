using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup
{
    public class ChoiceGroupValidationModel
    {
        [Required(ErrorMessage = "Pick one")]
        public string Value { get; set; }
    }
}
