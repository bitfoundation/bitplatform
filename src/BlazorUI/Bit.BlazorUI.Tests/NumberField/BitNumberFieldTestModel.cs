using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.NumberField;

public class BitNumberFieldTestModel
{
    [Range(6, 18)]
    public int? Value { get; set; }
}
