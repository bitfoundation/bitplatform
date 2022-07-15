namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/#common-configuration">here (Chart.js)</a>.
/// </summary>
public sealed class AxisDisplay : ObjectEnum
{
    /// <summary>
    /// Hidden
    /// </summary>
    public static AxisDisplay False => new AxisDisplay(false);

    /// <summary>
    /// Visible
    /// </summary>
    public static AxisDisplay True => new AxisDisplay(true);

    /// <summary>
    /// Visible only if at least one associated dataset is visible
    /// </summary>
    public static AxisDisplay Auto => new AxisDisplay("auto");


    private AxisDisplay(string stringValue) : base(stringValue) { }
    private AxisDisplay(bool boolValue) : base(boolValue) { }
}
