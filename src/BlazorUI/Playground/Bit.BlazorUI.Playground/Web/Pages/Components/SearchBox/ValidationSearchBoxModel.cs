using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.SearchBox
{
    public class ValidationSearchBoxModel
    {
        [StringLength(6, MinimumLength = 2,
        ErrorMessage = "The text field length must be between 6 and 2 characters in length.")]
        public string Text { get; set; }
    }
}
