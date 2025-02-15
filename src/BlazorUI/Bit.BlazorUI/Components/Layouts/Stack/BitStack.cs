using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A Stack is a container-type component that abstracts the implementation of a flexbox in order to define the layout of its children components.
/// </summary>
public partial class BitStack : BitComponentBase
{
    /// <summary>
    /// Defines whether to render Stack children both horizontally and vertically.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Make the height of the stack auto.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool AutoHeight { get; set; }

    /// <summary>
    /// Make the width and height of the stack auto.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool AutoSize { get; set; }

    /// <summary>
    /// Make the width of the stack auto.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool AutoWidth { get; set; }

    /// <summary>
    /// The content of the stack.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Expand the direct children to occupy all of the root element's width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FillContent { get; set; }

    /// <summary>
    /// Sets the height of the stack to fit its content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the stack to fit its content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitSize { get; set; }

    /// <summary>
    /// Sets the width of the stack to fit its content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitWidth { get; set; }

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
    public BitAlignment? HorizontalAlign { get; set; }

    /// <summary>
    /// Defines whether to render Stack children in the opposite direction (bottom-to-top if it's a vertical Stack and right-to-left if it's a horizontal Stack).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Defines whether to render Stack children vertically.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Defines whether Stack children should wrap onto multiple rows or columns when they are about to overflow the size of the Stack.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool Wrap { get; set; }



    protected override string RootElementClass => "bit-stc";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => FillContent ? "bit-stc-fcn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => "display:flex"); // to preserve display so it can't be overridden easily.

        StyleBuilder.Register(() => $"flex-direction:{(Horizontal ? "row" : "column")}{(Reversed ? "-reverse" : string.Empty)}");

        StyleBuilder.Register(() => $"gap:{Gap ?? "1rem"}");

        StyleBuilder.Register(() =>
        {
            var vAlign = VerticalAlign ?? Alignment;
            var hAlign = HorizontalAlign ?? Alignment;

            return (Horizontal && vAlign is not null) || (Horizontal is false && hAlign is not null)
                ? $"align-items:{_AlignmentMap[Horizontal ? vAlign!.Value : hAlign!.Value]}"
                : string.Empty;
        });

        StyleBuilder.Register(() =>
        {
            var vAlign = VerticalAlign ?? Alignment;
            var hAlign = HorizontalAlign ?? Alignment;

            return (Horizontal && hAlign is not null) || (Horizontal is false && vAlign is not null)
            ? $"justify-content:{_AlignmentMap[Horizontal ? hAlign!.Value : vAlign!.Value]}"
            : string.Empty;
        });

        StyleBuilder.Register(() => (Grow.HasValue() || Grows) ? $"flex-grow:{(Grow.HasValue() ? Grow : "1")}" : string.Empty);

        StyleBuilder.Register(() => Wrap ? "flex-wrap:wrap" : string.Empty);

        StyleBuilder.Register(() => (AutoSize || AutoWidth) ? "width:auto" : string.Empty);
        StyleBuilder.Register(() => (AutoSize || AutoHeight) ? "height:auto" : string.Empty);

        StyleBuilder.Register(() => (FitSize || FitWidth) ? "width:fit-content" : string.Empty);
        StyleBuilder.Register(() => (FitSize || FitHeight) ? "height:fit-content" : string.Empty);
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
