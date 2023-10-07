using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

public partial class BitStack : BitComponentBase
{
    private string? gap;
    private string? grow;
    private string? padding;
    private string? height;
    private string? width;
    private string? maxWidth;
    private string? maxHeight;
    private string? minWidth;
    private string? minHeight;
    private bool wrap;
    private bool grows;
    private bool reversed;
    private bool horizontal;
    private bool verticalFill;
    private bool disableShrink;
    private bool horizontalFill;
    private BitStackAlignment verticalAlign = BitStackAlignment.Start;
    private BitStackAlignment horizontalAlign = BitStackAlignment.Start;

    /// <summary>
    /// The content of the Typography.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Defines how to render the Stack.
    /// </summary>
    [Parameter] public string As { get; set; } = "div";

    /// <summary>
    /// Defines whether Stack children should not shrink to fit the available space.
    /// </summary>
    [Parameter] public bool DisableShrink
    {
        get => disableShrink;
        set
        {
            if (disableShrink == value) return;

            disableShrink = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the spacing between Stack children.The property is specified as a value for 'row gap', followed optionally by a value for 'column gap'. If 'column gap' is omitted, it's set to the same value as 'row gap'.
    /// </summary>
    [Parameter] public string? Gap
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
    [Parameter] public string? Grow
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
    [Parameter] public bool Grows
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
    /// Defines the height of the Stack.
    /// </summary>
    [Parameter]
    public string? Height
    {
        get => height;
        set
        {
            if (height == value) return;

            height = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    [Parameter] public bool Horizontal
    {
        get => horizontal;
        set
        {
            if (horizontal == value) return;

            horizontal = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Stack children horizontally.
    /// </summary>
    [Parameter] public BitStackAlignment HorizontalAlign
    {
        get => horizontalAlign;
        set
        {
            if (horizontalAlign == value) return;

            horizontalAlign = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether the Stack should take up 100% of the height of its parent. This property is required to be set to true when using the grow flag on children in vertical oriented Stacks. Stacks are rendered as block elements and grow horizontally to the container already.
    /// </summary>
    [Parameter]
    public bool HorizontalFill
    {
        get => horizontalFill;
        set
        {
            if (horizontalFill == value) return;

            horizontalFill = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the maximum height that the Stack can take.
    /// </summary>
    [Parameter] public string? MaxHeight
    {
        get => maxHeight;
        set
        {
            if (maxHeight == value) return;

            maxHeight = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the maximum width that the Stack can take.
    /// </summary>
    [Parameter] public string? MaxWidth
    {
        get => maxWidth;
        set
        {
            if (maxWidth == value) return;

            maxWidth = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the minimum height that the Stack can take.
    /// </summary>
    [Parameter] public string? MinHeight
    {
        get => minHeight;
        set
        {
            if (minHeight == value) return;

            minHeight = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the minimum width that the Stack can take.
    /// </summary>
    [Parameter] public string? MinWidth
    {
        get => minWidth;
        set
        {
            if (minWidth == value) return;

            minWidth = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the inner padding of the Stack.
    /// </summary>
    [Parameter] public string? Padding
    {
        get => padding;
        set
        {
            if (padding == value) return;

            padding = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Stack children in the opposite direction (bottom-to-top if it's a vertical Stack and right-to-left if it's a horizontal Stack).
    /// </summary>
    [Parameter] public bool Reversed
    {
        get => reversed;
        set
        {
            if (reversed == value) return;

            reversed = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether to render Stack children vertically.
    /// </summary>
    [Parameter] public BitStackAlignment VerticalAlign
    {
        get => verticalAlign;
        set
        {
            if (verticalAlign == value) return;

            verticalAlign = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether the Stack should take up 100% of the height of its parent. This property is required to be set to true when using the grow flag on children in vertical oriented Stacks. Stacks are rendered as block elements and grow horizontally to the container already.
    /// </summary>
    [Parameter] public bool VerticalFill
    {
        get => verticalFill;
        set
        {
            if (verticalFill == value) return;

            verticalFill = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines the width of the Stack.
    /// </summary>
    [Parameter]
    public string? Width
    {
        get => width;
        set
        {
            if (width == value) return;

            width = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Defines whether Stack children should wrap onto multiple rows or columns when they are about to overflow the size of the Stack.
    /// </summary>
    [Parameter] public bool Wrap
    {
        get => wrap;
        set
        {
            if (wrap == value) return;

            wrap = value;
            ClassBuilder.Reset();
        }
    }



    protected override string RootElementClass => "bit-stc";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => (DisableShrink ? $"{RootElementClass}-dsh" : string.Empty));

        ClassBuilder.Register(() => $"{RootElementClass}-{(Horizontal ? "hrz" : "vrt")}");

        ClassBuilder.Register(() => (Reversed ? $"{RootElementClass}-rvs" : string.Empty));
                                    
        ClassBuilder.Register(() => (Wrap ? $"{RootElementClass}-wrp" : string.Empty));

        ClassBuilder.Register(() => HorizontalAlign switch
        {
            BitStackAlignment.Start => $"{RootElementClass}-hst",
            BitStackAlignment.Center => $"{RootElementClass}-hct",
            BitStackAlignment.End => $"{RootElementClass}-hen",
            BitStackAlignment.SpaceBetween => $"{RootElementClass}-hsb",
            BitStackAlignment.SpaceAround => $"{RootElementClass}-hsa",
            BitStackAlignment.SpaceEvenly => $"{RootElementClass}-hse",
            BitStackAlignment.Baseline => $"{RootElementClass}-hbl",
            BitStackAlignment.Stretch => $"{RootElementClass}-hsr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => VerticalAlign switch
        {
            BitStackAlignment.Start => $"{RootElementClass}-vst",
            BitStackAlignment.Center => $"{RootElementClass}-vct",
            BitStackAlignment.End => $"{RootElementClass}-ven",
            BitStackAlignment.SpaceBetween => $"{RootElementClass}-vsb",
            BitStackAlignment.SpaceAround => $"{RootElementClass}-vsa",
            BitStackAlignment.SpaceEvenly => $"{RootElementClass}-vse",
            BitStackAlignment.Baseline => $"{RootElementClass}-vbl",
            BitStackAlignment.Stretch => $"{RootElementClass}-vsr",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Gap.HasValue() ? $"gap: {Gap}" : string.Empty);

        StyleBuilder.Register(() => (Grow.HasValue() || Grows) ? $"flex-grow: {(Grow.HasValue() ? Grow : "1")}" : string.Empty);

        StyleBuilder.Register(() => (Height.HasValue() || VerticalFill) ? $"height: {(Height.HasValue() ? Height : "100%")}" : string.Empty);

        StyleBuilder.Register(() => (Width.HasValue() || HorizontalFill) ? $"width: {(Width.HasValue() ? Width : "100%")}" : string.Empty);

        StyleBuilder.Register(() => MaxHeight.HasValue() ? $"max-height: {MaxHeight}" : string.Empty);

        StyleBuilder.Register(() => MaxWidth.HasValue() ? $"max-width: {MaxWidth}" : string.Empty);

        StyleBuilder.Register(() => MinHeight.HasValue() ? $"min-height: {MinHeight}" : string.Empty);

        StyleBuilder.Register(() => MinWidth.HasValue() ? $"min-width: {MinWidth}" : string.Empty);

        StyleBuilder.Register(() => Padding.HasValue() ? $"padding: {Padding}" : string.Empty);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        builder.OpenElement(seq++, As);
        builder.AddMultipleAttributes(seq++, RuntimeHelpers.TypeCheck<IEnumerable<KeyValuePair<string, object>>>(HtmlAttributes));
        builder.AddAttribute(seq++, "id", _Id);
        builder.AddAttribute(seq++, "style", StyleBuilder.Value);
        builder.AddAttribute(seq++, "class", ClassBuilder.Value);
        builder.AddElementReferenceCapture(seq++, v => RootElement = v);

        builder.AddContent(seq++, ChildContent);

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
