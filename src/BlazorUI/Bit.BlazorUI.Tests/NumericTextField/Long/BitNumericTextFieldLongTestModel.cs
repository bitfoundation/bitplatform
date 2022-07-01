using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.Long;

public class BitNumericTextFieldLongTestModel
{
    [Range(6, 18)]
    public long? Value { get; set; }
}
