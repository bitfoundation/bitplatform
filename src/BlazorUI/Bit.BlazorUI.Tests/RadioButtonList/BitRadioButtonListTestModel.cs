using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.RadioButtonList
{
    public class BitRadioButtonListTestModel
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Value { get; set; }
    }
}
