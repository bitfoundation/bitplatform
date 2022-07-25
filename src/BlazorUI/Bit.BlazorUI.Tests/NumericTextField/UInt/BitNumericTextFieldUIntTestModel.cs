using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.UInt;

public class BitNumericTextFieldUIntTestModel
{
    [Range(6, 18)]
    public uint? Value { get; set; }
}
