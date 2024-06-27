using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.SpinButton;

public class BitSpinButtonTestModel
{
    [Range(6, 18)]
    public double Value { get; set; }
}
