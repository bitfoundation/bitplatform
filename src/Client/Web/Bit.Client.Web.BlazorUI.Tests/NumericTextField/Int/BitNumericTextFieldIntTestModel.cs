using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.NumericTextField.Int
{
    public class BitNumericTextFieldIntTestModel
    {
        [Range(6, 18)]
        public int? Value { get; set; }
    }
}
