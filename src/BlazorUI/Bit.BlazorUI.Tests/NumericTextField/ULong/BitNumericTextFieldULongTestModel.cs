using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.ULong;

public class BitNumericTextFieldULongTestModel
{
    [Range(6, 18)]
    public ulong? Value { get; set; }
}
