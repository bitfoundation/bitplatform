namespace Bit.BlazorUI;

/// <summary>Title / subtitle plugin options.</summary>
public sealed class BitChartTitleOptions
{
    public bool Display { get; set; }
    public string Text { get; set; } = "";
    public string Color { get; set; } = "var(--bit-Chart-title-color, var(--bit-clr-fg-pri))";
    /// <summary>
    /// Which edge of the chart the title is drawn against (default is the top).
    /// </summary>
    /// <remarks>
    /// Only Top, Bottom, Left, Right, Start and End are meaningful here. Top and Bottom draw the title as a band
    /// across the chart; the other four run it down the side of the plot, turned on its side. Left and Right stay on
    /// the side they are named for in both reading directions, while Start and End follow it: a title at the Start
    /// sits on the left of a left-to-right chart and on the right of a right-to-left one. Every other value leaves
    /// the title at the top. Use <see cref="Align"/> to move it along the edge it is drawn against.
    /// </remarks>
    public BitPlacement Placement { get; set; } = BitPlacement.Top;
    /// <summary>
    /// Where the title lines up along the edge it is drawn against (default is the center).
    /// </summary>
    /// <remarks>
    /// Only Start, Center, End, Left and Right are meaningful here. Start and End follow the reading direction, while
    /// Left and Right stay on the same side of the screen in both; every other value centers the title.
    /// </remarks>
    public BitPlacement Align { get; set; } = BitPlacement.Center;
    public BitChartFont Font { get; set; } = new() { Size = 16, Weight = "bold" };
    public BitChartPadding Padding { get; set; } = 10;
}
