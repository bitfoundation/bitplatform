using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Checkbox;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitCheckboxTestModel
{
    [Range(typeof(bool), "false", "false")]
    public bool Value { get; set; }
}
