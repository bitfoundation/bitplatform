using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitToggleTestModel
{
    [Range(typeof(bool), "false", "false")]
    public bool Value { get; set; }
}
