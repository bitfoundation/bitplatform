namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#ticks-source">here (Chart.js)</a>..
/// </summary>
public sealed class TickSource : StringEnum
{
    /// <summary>
    /// Generates "optimal" ticks based on scale size and time options.
    /// </summary>
    public static TickSource Auto => new TickSource("auto");

    /// <summary>
    /// Generates ticks from data (including labels from data {t|x|y} objects).
    /// </summary>
    public static TickSource Data => new TickSource("data");

    /// <summary>
    /// Generates ticks from user given <see cref="ChartData.Labels"/> values ONLY.
    /// </summary>
    public static TickSource Labels => new TickSource("labels");


    private TickSource(string stringRep) : base(stringRep) { }
}
