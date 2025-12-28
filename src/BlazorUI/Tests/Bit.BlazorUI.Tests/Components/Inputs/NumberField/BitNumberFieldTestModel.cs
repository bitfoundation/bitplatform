using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.NumberField;

public class BitNumberFieldTestModel
{
    [Range(6, 18)]
    public int? Value { get; set; }
}
