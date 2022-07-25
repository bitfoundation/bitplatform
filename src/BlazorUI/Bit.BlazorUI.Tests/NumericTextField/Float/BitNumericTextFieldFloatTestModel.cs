using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.Float;

public class BitNumericTextFieldFloatTestModel
{
    [Range(6, 18)]
    public float? Value { get; set; }
}
