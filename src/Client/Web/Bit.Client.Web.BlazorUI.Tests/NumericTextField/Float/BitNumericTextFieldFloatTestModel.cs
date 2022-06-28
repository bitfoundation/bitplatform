using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.NumericTextField.Float
{
    public class BitNumericTextFieldFloatTestModel
    {
        [Range(6, 18)]
        public float? Value { get; set; }
    }
}
