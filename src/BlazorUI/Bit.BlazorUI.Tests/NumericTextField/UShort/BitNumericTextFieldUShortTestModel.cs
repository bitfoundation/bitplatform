using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.UShort;

public class BitNumericTextFieldUShortTestModel
{
    [Range(6, 18)]
    public ushort? Value { get; set; }
}
