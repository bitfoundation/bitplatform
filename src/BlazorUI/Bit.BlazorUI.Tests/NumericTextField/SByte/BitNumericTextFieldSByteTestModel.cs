using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.SByte;

public class BitNumericTextFieldSByteTestModel
{
    [Range(6, 18)]
    public sbyte? Value { get; set; }
}
