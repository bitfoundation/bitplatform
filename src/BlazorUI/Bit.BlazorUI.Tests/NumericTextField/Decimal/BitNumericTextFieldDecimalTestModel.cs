using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumericTextField.Decimal;

public class BitNumericTextFieldDecimalTestModel
{
    [Range(6, 18)]
    public decimal? Value { get; set; }
}
