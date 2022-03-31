using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.DropDown
{
    public class FormValidationDropDownModel
    {
        [MaxLength(2, ErrorMessage = "The property {0} doesn't have more than {1} elements")]
        [MinLength(1, ErrorMessage = "The property {0} doesn't have less than {1} elements")]
        public List<string> Products { get; set; } = new();

        [Required]
        public string Category { get; set; }
    }
}
