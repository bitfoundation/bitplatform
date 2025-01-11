namespace Bit.BlazorUI;

/// <summary>
/// The BitGrid component is a flexible and customizable grid layout, offering responsive columns and alignment flexibility for structured content presentation.
/// </summary>
public partial class BitGrid : BitComponentBase
{
    /// <summary>
    /// The content of the Grid.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Defines the columns of Grid.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int Columns { get; set; } = 12;

    /// <summary>
    /// Number of columns in the extra small breakpoint.
    /// </summary>
    [Parameter] public int? ColumnsXs { get; set; }

    /// <summary>
    ///Number of columns in the small breakpoint.
    /// </summary>
    [Parameter] public int? ColumnsSm { get; set; }

    /// <summary>
    /// Number of columns in the medium breakpoint.
    /// </summary>
    [Parameter] public int? ColumnsMd { get; set; }

    /// <summary>
    /// Number of columns in the large breakpoint.
    /// </summary>
    [Parameter] public int? ColumnsLg { get; set; }

    /// <summary>
    /// Number of columns in the extra large breakpoint.
    /// </summary>
    [Parameter] public int? ColumnsXl { get; set; }

    /// <summary>
    /// Number of columns in the extra extra large breakpoint.
    /// </summary>
    [Parameter] public int? ColumnsXxl { get; set; }

    /// <summary>
    /// Defines whether to render Grid children horizontally.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment HorizontalAlign { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between Grid children.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacing { get; set; }

    /// <summary>
    /// Defines the spacing between Grid children.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string Spacing { get; set; } = "4px";

    /// <summary>
    /// Defines the span of Grid.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int Span { get; set; } = 1;

    /// <summary>
    /// Defines the vertical spacing between Grid children.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacing { get; set; }



    protected override string RootElementClass => "bit-grd";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => $"justify-content:{_AlignmentMap[HorizontalAlign]}");

        StyleBuilder.Register(() => $"--span:{Span}");
        StyleBuilder.Register(() => $"--columns:{Columns}");
        StyleBuilder.Register(() => $"--spacing:{HorizontalSpacing ?? Spacing}");

        StyleBuilder.Register(() => VerticalSpacing.HasValue() ? $"row-gap:{VerticalSpacing}" : string.Empty);
        StyleBuilder.Register(() => HorizontalSpacing.HasValue() ? $"column-gap:{HorizontalSpacing}" : string.Empty);
    }



    private static readonly Dictionary<BitAlignment, string> _AlignmentMap = new()
    {
        { BitAlignment.Start, "flex-start" },
        { BitAlignment.End, "flex-end" },
        { BitAlignment.Center, "center" },
        { BitAlignment.SpaceBetween, "space-between" },
        { BitAlignment.SpaceAround, "space-around" },
        { BitAlignment.SpaceEvenly, "space-evenly" },
        { BitAlignment.Baseline, "baseline" },
        { BitAlignment.Stretch, "stretch" },
    };
}
