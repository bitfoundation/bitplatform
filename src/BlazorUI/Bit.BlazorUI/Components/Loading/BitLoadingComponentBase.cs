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
    [Parameter] public string Color { get; set; } = "#FFFFFF";

    protected virtual int OriginalSize { get; set; } = 80;

    protected double Convert(double value)
    {
        return value * Size / OriginalSize;
    }
}
