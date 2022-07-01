using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.Double;

public class BitNumericTextFieldDoubleTestModel
{
    [Range(6, 18)]
    public double? Value { get; set; }
}
