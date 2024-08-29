using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

public partial class BitStack : BitComponentBase
{
    /// <summary>
    /// The content of the Typography.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Defines the spacing between Stack children.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// Defines how much to grow the Stack in proportion to its siblings.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Grow { get; set; }

    /// <summary>
    /// Defines how much to grow the Stack in proportion to its siblings.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool Grows { get; set; }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitStackAlignment HorizontalAlign { get; set; }

    /// <summary>
    /// Make the width and height of the stack 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool Full { get; set; }

    /// <summary>
    /// Make the height of the stack 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Make the width of the stack 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Defines whether to render Stack children in the opposite direction (bottom-to-top if it's a vertical Stack and right-to-left if it's a horizontal Stack).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Defines whether to render Stack children vertically.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitStackAlignment VerticalAlign { get; set; }

    /// <summary>
    /// Defines whether Stack children should wrap onto multiple rows or columns when they are about to overflow the size of the Stack.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool Wrap { get; set; }



    protected override string RootElementClass => "bit-stc";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => $"flex-direction:{(Horizontal ? "row" : "column")}{(Reversed ? "-reverse" : string.Empty)}");

        StyleBuilder.Register(() => $"align-items:{_AlignmentMap[Horizontal ? VerticalAlign : HorizontalAlign]}");

        StyleBuilder.Register(() => $"justify-content:{_AlignmentMap[Horizontal ? HorizontalAlign : VerticalAlign]}");

        StyleBuilder.Register(() => Gap.HasValue() ? $"gap:{Gap}" : string.Empty);

        StyleBuilder.Register(() => (Grow.HasValue() || Grows) ? $"flex-grow:{(Grow.HasValue() ? Grow : "1")}" : string.Empty);

        StyleBuilder.Register(() => Wrap ? "flex-wrap:wrap" : string.Empty);

        StyleBuilder.Register(() => (Full || FullWidth) ? "width:100%" : string.Empty);
        StyleBuilder.Register(() => (Full || FullHeight) ? "height:100%" : string.Empty);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? "div");
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
}
