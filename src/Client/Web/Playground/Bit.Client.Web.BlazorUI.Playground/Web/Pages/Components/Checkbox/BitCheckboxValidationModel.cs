using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Checkbox
{
    public class BitCheckboxValidationModel
    {
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions.")]
        public bool TermsAgreement { get; set; }
    }
}
