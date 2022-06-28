using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.Inputs
{
    public class BitTextFieldTestModel
    {
        [Required]
        [EmailAddress]
        [StringLength(16)]
        public string Value { get; set; }
    }
}
