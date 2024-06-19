namespace Bit.BlazorUI;

public partial class BitGrid : BitComponentBase
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
    private BitGridAlignment verticalAlign = BitGridAlignment.Start;
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
    /// Defines whether to render Grid children vertically.
    /// </summary>
    [Parameter]
    public BitGridAlignment VerticalAlign
    {
        get => verticalAlign;
        set
        {
            if (verticalAlign == value) return;

            verticalAlign = value;
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
            register($"align-items:{_AlignmentMap[VerticalAlign]}");
            register($"justify-content:{_AlignmentMap[HorizontalAlign]}");

            return string.Empty;
        });

        StyleBuilder.Register(() => $"--span:{Span}");
        StyleBuilder.Register(() => $"--columns:{Columns}");
        StyleBuilder.Register(() => $"--spacing:{HorizontalSpacing ?? Spacing}");

        StyleBuilder.Register(() => VerticalSpacing.HasValue() ? $"row-gap:{VerticalSpacing}" : string.Empty);
        StyleBuilder.Register(() => HorizontalSpacing.HasValue() ? $"column-gap:{HorizontalSpacing}" : string.Empty);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        builder.OpenComponent<CascadingValue<BitGrid>>(seq++);
        builder.AddAttribute(seq++, "Value", this);
        builder.AddAttribute(seq++, "IsFixed", true);
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)((childBuilder) =>
        {
            var seqChild = 0;
            childBuilder.OpenElement(seqChild++, "div");
            childBuilder.AddMultipleAttributes(seqChild++, Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<IEnumerable<KeyValuePair<string, object>>>(HtmlAttributes));
            childBuilder.AddAttribute(seqChild++, "id", _Id);
            childBuilder.AddAttribute(seqChild++, "style", StyleBuilder.Value);
            childBuilder.AddAttribute(seqChild++, "class", ClassBuilder.Value);
            childBuilder.AddAttribute(seqChild++, "dir", Dir?.ToString().ToLower());
            childBuilder.AddElementReferenceCapture(seqChild++, v => RootElement = v);
            childBuilder.AddContent(seqChild++, ChildContent);
            childBuilder.CloseElement();
        }));
        builder.CloseComponent();

        base.BuildRenderTree(builder);
    }
}
