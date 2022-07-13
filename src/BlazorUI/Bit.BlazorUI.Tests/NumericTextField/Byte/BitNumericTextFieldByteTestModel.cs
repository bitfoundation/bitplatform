using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.Byte;

public class BitNumericTextFieldByteTestModel
{
    [Range(6, 18)]
    public byte? Value { get; set; }
}
