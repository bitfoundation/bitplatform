namespace Bit.BlazorUI;

/// <summary>
/// Specifies the border alignment of a pie chart and a polar area chart.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/doughnut.html#border-alignment">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BorderAlign : StringEnum
{
    /// <summary>
    /// When <see cref="BorderAlign.Center" /> is set, the borders of arcs next to each other will overlap. The default value.
    /// </summary>
    public static BorderAlign Center => new BorderAlign("center");

    /// <summary>
    /// When <see cref="BorderAlign.Inner" /> is set, it is guaranteed that all the borders will not overlap.
    /// </summary>
    public static BorderAlign Inner => new BorderAlign("inner");

    /// <summary>
    /// Creates a new instance of the <see cref="BorderAlign"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BorderAlign(string stringValue) : base(stringValue) { }
}
