using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

public class BitToggleTestModel
{
    [Range(typeof(bool), "false", "false")]
    public bool Value { get; set; }
}
