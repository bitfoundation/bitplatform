using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Checkbox;

public class BitCheckboxTestModel
{
    [Range(typeof(bool), "false", "false")]
    public bool Value { get; set; }
}
