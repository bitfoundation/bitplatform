using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.RadioButtonList
{
    public class FormValidationModel
    {
        [Required]
        public int? GenderId { get; set; }
    }
}
