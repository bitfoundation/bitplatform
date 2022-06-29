using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Inputs
{
    public class BitTextFieldTestModel
    {
        [Required]
        [EmailAddress]
        [StringLength(16)]
        public string Value { get; set; }
    }
}
