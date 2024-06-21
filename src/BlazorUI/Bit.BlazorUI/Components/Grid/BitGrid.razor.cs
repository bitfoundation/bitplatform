using System.Reflection.Emit;

namespace Bit.BlazorUI;

public partial class BitGrid
{
    private static readonly Dictionary<BitGridAlignment, string> _AlignmentMap = new()
    {
        { BitGridAlignment.Start, "flex-start" },
        { BitGridAlignment.End, "flex-end" },
        { BitGridAlignment.Center, "center" },
        { BitGridAlignment.SpaceBetween, "space-between" },
        { BitGridAlignment.SpaceAround, "space-around" },
        { BitGridAlignment.SpaceEvenly, "space-evenly" },
        { BitGridAlignment.Baseline, "baseline" },
        { BitGridAlignment.Stretch, "stretch" },
    };


    private int span = 1;
    private int columns = 12;
    private string spacing = "4px";
    private string? verticalSpacing;
    private string? horizontalSpacing;
    private BitGridAlignment horizontalAlign = BitGridAlignment.Start;



    /// <summary>
    /// The content of the Grid.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Defines the columns of Grid.
    /// </summary>
    [Parameter]
    public int Columns
    {
        get => columns;
        set
        {
            if (columns == value) return;

            columns = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Grid children horizontally.
    /// </summary>
    [Parameter]
    public BitGridAlignment HorizontalAlign
    {
        get => horizontalAlign;
        set
        {
            if (horizontalAlign == value) return;

            horizontalAlign = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the horizontal spacing between Grid children.
    /// </summary>
    [Parameter]
    public string? HorizontalSpacing
    {
        get => horizontalSpacing;
        set
        {
            if (horizontalSpacing == value) return;

            horizontalSpacing = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the spacing between Grid children.
    /// </summary>
    [Parameter]
    public string Spacing
    {
        get => spacing;
        set
        {
            if (spacing == value) return;

            spacing = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the span of Grid.
    /// </summary>
    [Parameter]
    public int Span
    {
        get => span;
        set
        {
            if (span == value) return;

            span = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the vertical spacing between Grid children.
    /// </summary>
    [Parameter]
    public string? VerticalSpacing
    {
        get => verticalSpacing;
        set
        {
            if (verticalSpacing == value) return;

            verticalSpacing = value;
            StyleBuilder.Reset();
        }
    }


    protected override string RootElementClass => "bit-grd";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(register =>
        {
            register($"justify-content:{_AlignmentMap[HorizontalAlign]}");

            return string.Empty;
        });

        StyleBuilder.Register(() => $"--span:{Span}");
        StyleBuilder.Register(() => $"--columns:{Columns}");
        StyleBuilder.Register(() => $"--spacing:{HorizontalSpacing ?? Spacing}");

        StyleBuilder.Register(() => VerticalSpacing.HasValue() ? $"row-gap:{VerticalSpacing}" : string.Empty);
        StyleBuilder.Register(() => HorizontalSpacing.HasValue() ? $"column-gap:{HorizontalSpacing}" : string.Empty);
    }
}
