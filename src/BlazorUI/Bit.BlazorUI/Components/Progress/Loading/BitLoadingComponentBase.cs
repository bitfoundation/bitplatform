using System.Globalization;

namespace Bit.BlazorUI;

public class BitLoadingComponentBase : ComponentBase
{
    /// <summary>
    /// The Size of the loading component in px.
    /// </summary>
    [Parameter] public int Size { get; set; } = 64;

    /// <summary>
    /// The Color of the loading component compatible with colors in CSS.
    /// </summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>
    /// Custom CSS class for the root element of the component.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Custom CSS style for the root element of the component.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Custom CSS class for the child element(s) of the component.
    /// </summary>
    [Parameter] public string? ChildClass { get; set; }

    /// <summary>
    /// Custom CSS style for the child element(s) of the component.
    /// </summary>
    [Parameter] public string? ChildStyle { get; set; }



    protected virtual int OriginalSize { get; set; } = 80;

    protected string Convert(double value)
    {
        return (value * Size / OriginalSize).ToString(CultureInfo.InvariantCulture);
    }
}
