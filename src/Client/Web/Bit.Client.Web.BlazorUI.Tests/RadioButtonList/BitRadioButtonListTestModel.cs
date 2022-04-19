using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.RadioButtonList
{
    public class BitRadioButtonListTestModel
    {
        [Required]
        [Range(1,int.MaxValue)]
        public int Value { get; set; }
    }
}
