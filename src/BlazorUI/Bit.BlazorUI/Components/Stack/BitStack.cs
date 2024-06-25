using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

public partial class BitStack : BitComponentBase
{
    private static readonly Dictionary<BitStackAlignment, string> _AlignmentMap = new()
    {
        { BitStackAlignment.Start, "flex-start" },
        { BitStackAlignment.End, "flex-end" },
        { BitStackAlignment.Center, "center" },
        { BitStackAlignment.SpaceBetween, "space-between" },
        { BitStackAlignment.SpaceAround, "space-around" },
        { BitStackAlignment.SpaceEvenly, "space-evenly" },
        { BitStackAlignment.Baseline, "baseline" },
        { BitStackAlignment.Stretch, "stretch" },
    };


    private string? gap;
    private string? grow;
    private bool wrap;
    private bool grows;
    private bool reversed;
    private bool horizontal;
    private bool disableShrink;
    private BitStackAlignment verticalAlign = BitStackAlignment.Start;
    private BitStackAlignment horizontalAlign = BitStackAlignment.Start;



    /// <summary>
    /// Defines how to render the Stack.
    /// </summary>
    [Parameter] public string As { get; set; } = "div";

    /// <summary>
    /// The content of the Typography.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Defines the spacing between Stack children.
    /// </summary>
    [Parameter]
    public string? Gap
    {
        get => gap;
        set
        {
            if (gap == value) return;

            gap = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines how much to grow the Stack in proportion to its siblings.
    /// </summary>
    [Parameter]
    public string? Grow
    {
        get => grow;
        set
        {
            if (grow == value) return;

            grow = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines how much to grow the Stack in proportion to its siblings.
    /// </summary>
    [Parameter]
    public bool Grows
    {
        get => grows;
        set
        {
            if (grows == value) return;

            grows = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    [Parameter]
    public bool Horizontal
    {
        get => horizontal;
        set
        {
            if (horizontal == value) return;

            horizontal = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    [Parameter]
    public BitStackAlignment HorizontalAlign
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
    /// Defines whether to render Stack children in the opposite direction (bottom-to-top if it's a vertical Stack and right-to-left if it's a horizontal Stack).
    /// </summary>
    [Parameter]
    public bool Reversed
    {
        get => reversed;
        set
        {
            if (reversed == value) return;

            reversed = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Stack children vertically.
    /// </summary>
    [Parameter]
    public BitStackAlignment VerticalAlign
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
    /// Defines whether Stack children should wrap onto multiple rows or columns when they are about to overflow the size of the Stack.
    /// </summary>
    [Parameter]
    public bool Wrap
    {
        get => wrap;
        set
        {
            if (wrap == value) return;

            wrap = value;
            StyleBuilder.Reset();
        }
    }



    protected override string RootElementClass => "bit-stc";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(register =>
        {
            register($"flex-direction:{(Horizontal ? "row" : "column")}{(Reversed ? "-reverse" : "")}");

            register($"align-items:{_AlignmentMap[Horizontal ? VerticalAlign : HorizontalAlign]}");
            register($"justify-content:{_AlignmentMap[Horizontal ? HorizontalAlign : VerticalAlign]}");

            return string.Empty;
        });

        StyleBuilder.Register(() => Gap.HasValue() ? $"gap:{Gap}" : string.Empty);

        StyleBuilder.Register(() => (Grow.HasValue() || Grows) ? $"flex-grow:{(Grow.HasValue() ? Grow : "1")}" : string.Empty);

        StyleBuilder.Register(() => Wrap ? "flex-wrap:wrap" : string.Empty);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As);
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower());
        builder.AddElementReferenceCapture(6, v => RootElement = v);
        builder.AddContent(7, ChildContent);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
