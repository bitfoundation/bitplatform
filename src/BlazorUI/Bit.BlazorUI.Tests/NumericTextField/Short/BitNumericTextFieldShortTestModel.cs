using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.Short;

public class BitNumericTextFieldShortTestModel
{
    [Range(6, 18)]
    public short? Value { get; set; }
}
