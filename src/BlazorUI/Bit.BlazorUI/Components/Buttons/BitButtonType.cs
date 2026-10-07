using System.ComponentModel;

namespace Bit.BlazorUI;

/// <summary>
/// Defines the type attribute of the rendered button element, which decides what clicking it does inside a form.
/// </summary>
[DefaultValue(Button)]
public enum BitButtonType
{
    /// <summary>
    /// The button is a clickable button.
    /// </summary>
    Button,

    /// <summary>
    /// The button is a submit button (submits form-data).
    /// </summary>
    Submit,

    /// <summary>
    /// The button is a reset button (resets the form-data to its initial values).
    /// </summary>
    Reset
}

public static class BitButtonTypeExtensions
{
    public static string GetValue(this BitButtonType bitButtonType)
    {
        return bitButtonType switch
        {
            BitButtonType.Button => "button",
            BitButtonType.Submit => "submit",
            BitButtonType.Reset => "reset",
            _ => string.Empty,
        };
    }
}
