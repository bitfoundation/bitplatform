namespace Bit.BlazorUI;

public class BitColorChangeEventArgs
{
    public void Deconstruct(out string? color, out double alpha)
    {
        color = Color;
        alpha = Alpha;
    }

    /// <summary>
    /// The main color value of the changed color in the same format as the Color parameter of the ColorPicker.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// The alpha value of the changed color.
    /// </summary>
    public double Alpha { get; set; }
}
