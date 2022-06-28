using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Tests.NumericTextField.Double
{
    public class BitNumericTextFieldDoubleTestModel
    {
        [Range(6, 18)]
        public double? Value { get; set; }
    }
}
