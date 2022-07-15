namespace Bit.BlazorUI;

/// <summary>
/// Specifies the scale boundary strategy.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#scale-bounds">here (Chart.js)</a>.</para>
/// </summary>
public sealed class ScaleBound : StringEnum
{
    /// <summary>
    /// Makes sure data are fully visible, labels outside are removed.
    /// </summary>
    public static ScaleBound Data => new ScaleBound("data");

    /// <summary>
    /// Makes sure ticks are fully visible, data outside are truncated.
    /// </summary>
    public static ScaleBound Ticks => new ScaleBound("ticks");

    private ScaleBound(string stringRep) : base(stringRep) { }
}
