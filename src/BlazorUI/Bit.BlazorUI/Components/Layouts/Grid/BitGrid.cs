using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// The BitGrid component is a flexible and customizable grid layout, offering responsive columns and alignment flexibility for structured content presentation.
/// </summary>
/// <remarks>
/// The grid divides the width it is given into <see cref="Columns"/> equal tracks (12 by default) and lays its
/// <see cref="BitGridItem"/> children out over those tracks, wrapping onto a new row whenever the next item no
/// longer fits. Each item spans one track by default and states how many it wants with
/// <see cref="BitGridItem.ColumnSpan"/>, which makes the classic "two thirds next to one third" content layout a
/// single number on each of two items.
/// <br />
/// Everything about that layout is responsive: the grid takes a different column count per breakpoint
/// (<see cref="ColumnsXs"/> to <see cref="ColumnsXxl"/>) and every item takes a different span, offset and order
/// per breakpoint. Those values are mobile first - a value set at one breakpoint keeps applying to every wider
/// one until another value replaces it - so a three column desktop layout collapses to a single column phone
/// layout without repeating itself at every size in between.
/// </remarks>
public partial class BitGrid : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the grid component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple grid components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitGridParams.ParamName)]
    public BitGridParams? CascadingParameters { get; set; }



    /// <summary>
    /// Defines how the rows of a wrapping BitGrid share out the height left over (the CSS align-content of the container).
    /// </summary>
    /// <remarks>
    /// This is the only alignment that acts on the rows themselves rather than on the items within a row, so it
    /// only becomes visible when the grid is taller than the rows it produced and there is more than one of them.
    /// A grid that is not taller than its content, or one that is kept on a single row by <see cref="NoWrap"/>,
    /// has nothing left over to share out and is unaffected.
    /// <br />
    /// Every member of <see cref="BitAlignment"/> means something here: Start, Center and End park the block of
    /// rows at one end of the grid, the three space distributions spread the rows apart, and Stretch grows the
    /// rows themselves until they fill the height.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? AlignContent { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the children of the BitGrid on both axes at once.
    /// </summary>
    /// <remarks>
    /// This is the shorthand of setting <see cref="HorizontalAlign"/> and <see cref="VerticalAlign"/> to the same
    /// value, and each of those takes precedence over it on its own axis.
    /// <br />
    /// The three space distributions of <see cref="BitAlignment"/> only mean something on the horizontal axis and
    /// Baseline only means something on the vertical one, so those members reach a single axis through this shorthand.
    /// <br />
    /// <see cref="AlignContent"/> is left alone: it aligns the rows of the grid rather than its children, so it
    /// is not one of the two axes this shorthand stands for.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// The content of the Grid.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Defines the number of columns the width of the BitGrid is divided into.
    /// <br />
    /// The default value is <strong>12</strong>.
    /// </summary>
    /// <remarks>
    /// A <see cref="BitGridItem"/> asks for a number of these columns with its
    /// <see cref="BitGridItem.ColumnSpan"/>, so the column count is the denominator every item is measured against:
    /// 12 columns make halves, thirds, quarters and sixths all expressible, while a grid of equal cards is easier
    /// to read as 3 or 4 columns of one span each.
    /// <br />
    /// This is the count of every breakpoint that is not overridden, and the per breakpoint counts
    /// (<see cref="ColumnsXs"/> to <see cref="ColumnsXxl"/>) replace it from their own breakpoint upwards.
    /// <br />
    /// Values below 1 are treated as 1, since a width cannot be divided into no columns at all.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int Columns { get; set; } = 12;

    /// <summary>
    /// Number of columns in the extra small breakpoint (from 0px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ColumnsXs { get; set; }

    /// <summary>
    /// Number of columns in the small breakpoint (from 600px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ColumnsSm { get; set; }

    /// <summary>
    /// Number of columns in the medium breakpoint (from 960px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ColumnsMd { get; set; }

    /// <summary>
    /// Number of columns in the large breakpoint (from 1280px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ColumnsLg { get; set; }

    /// <summary>
    /// Number of columns in the extra large breakpoint (from 1920px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ColumnsXl { get; set; }

    /// <summary>
    /// Number of columns in the extra extra large breakpoint (from 2560px).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? ColumnsXxl { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// A grid of things that form a list is more meaningful as a <c>ul</c> whose children are rendered as <c>li</c>
    /// through <see cref="BitGridItem.Element"/>, and a grid that is a region of the page can be a <c>section</c>.
    /// The layout itself is unaffected: the element only changes the tag name, and therefore the semantics reported
    /// to assistive technologies.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Defines the horizontal distribution of the children of the BitGrid (the CSS justify-content of the container).
    /// </summary>
    /// <remarks>
    /// This only becomes visible when the items of a row do not use up the full width of the grid, which is what a
    /// row whose spans do not add up to the column count leaves behind.
    /// <br />
    /// Baseline and Stretch say nothing about distributing free space, so those two act on the vertical axis
    /// instead, exactly like <see cref="VerticalAlign"/>, which takes precedence over them when it is set.
    /// <br />
    /// When not set, the items are packed against the start edge of the grid.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? HorizontalAlign { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the BitGrid.
    /// </summary>
    /// <remarks>
    /// Takes any CSS length (for example <c>0.5rem</c>, <c>8px</c> or <c>2%</c>), including a fluid one such as
    /// <c>clamp(4px, 1vw, 16px)</c>, which is how the spacing follows the size of the viewport without a
    /// breakpoint of its own. A bare number is read as pixels.
    /// <br />
    /// The horizontal spacing is part of the width of every item: the free width of a row is what is left after
    /// the gaps between its items are taken out, and that remainder is what the columns divide up. Widening the
    /// spacing therefore narrows the items instead of pushing them out of the row.
    /// <br />
    /// When not set, <see cref="Spacing"/> is used for this axis too.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacing { get; set; }

    /// <summary>
    /// Keeps the children of the BitGrid on a single row instead of letting them wrap onto more rows.
    /// </summary>
    /// <remarks>
    /// The items of a row that is asked to hold more than its column count are shrunk to fit rather than moved to
    /// the next row, so this turns the grid into a single line whose proportions are the spans of its items. It is
    /// what a toolbar or a summary strip that must never break wants.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// Renders the children of the BitGrid in the opposite direction (right to left in a left-to-right grid, and
    /// left to right in a right-to-left one).
    /// </summary>
    /// <remarks>
    /// Only the painted order is reversed; the order the items are read in by a screen reader and reached in by
    /// the keyboard stays the order they are written in, so this is a visual tool and not a way to reorder content.
    /// <br />
    /// <see cref="BitGridItem.Offset"/> follows the text direction rather than this, so it keeps indenting from
    /// the left of a reversed left-to-right grid, which is the trailing edge of a reversed row.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the BitGrid on both axes.
    /// <br />
    /// The default value is <strong>4px</strong>.
    /// </summary>
    /// <remarks>
    /// Takes any CSS length (for example <c>0.5rem</c>, <c>8px</c> or <c>2%</c>), including a fluid one such as
    /// <c>clamp(4px, 1vw, 16px)</c>, which is how the spacing follows the size of the viewport without a
    /// breakpoint of its own. A bare number is read as pixels.
    /// <br />
    /// <see cref="HorizontalSpacing"/> and <see cref="VerticalSpacing"/> each replace it on their own axis, which
    /// is what a grid of cards that needs more air between its rows than between its columns wants.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string Spacing { get; set; } = "4px";

    /// <summary>
    /// Defines the number of columns the children of the BitGrid fill by default.
    /// <br />
    /// The default value is <strong>1</strong>.
    /// </summary>
    /// <remarks>
    /// Every <see cref="BitGridItem"/> that does not state a <see cref="BitGridItem.ColumnSpan"/> of its own falls
    /// back to this, so a grid of uniform items states the span once on the grid instead of once on each item.
    /// <br />
    /// It is the base of the mobile first chain of an item too: an item that only sets a span for a wider
    /// breakpoint uses this one below that breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int Span { get; set; } = 1;

    /// <summary>
    /// Defines the vertical alignment of the children of the BitGrid within a row (the CSS align-items of the container).
    /// </summary>
    /// <remarks>
    /// A row is as tall as its tallest item, and this decides what the shorter ones do with the height left over:
    /// Start (the default) keeps them at the top of the row, Center centers them, End drops them to the bottom,
    /// Stretch makes them all as tall as the row - which is what a row of cards that should end on the same line
    /// wants - and Baseline lines up the first line of text of each of them.
    /// <br />
    /// The three space distributions of <see cref="BitAlignment"/> have no meaning on this axis and are ignored.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the BitGrid.
    /// </summary>
    /// <remarks>
    /// Takes any CSS length (for example <c>0.5rem</c>, <c>8px</c> or <c>2%</c>), including a fluid one such as
    /// <c>clamp(4px, 1vw, 16px)</c>. A bare number is read as pixels.
    /// <br />
    /// Unlike <see cref="HorizontalSpacing"/> this has no effect on the width of the items: it only separates the
    /// rows a wrapping grid produces.
    /// <br />
    /// When not set, <see cref="Spacing"/> is used for this axis too.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacing { get; set; }



    protected override string RootElementClass => "bit-grd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => NoWrap ? "bit-grd-nwr" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-grd-rev" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => _JustifyContent switch
        {
            BitAlignment.Start => "justify-content:flex-start",
            BitAlignment.End => "justify-content:flex-end",
            BitAlignment.Center => "justify-content:center",
            BitAlignment.SpaceBetween => "justify-content:space-between",
            BitAlignment.SpaceAround => "justify-content:space-around",
            BitAlignment.SpaceEvenly => "justify-content:space-evenly",
            _ => string.Empty
        });

        StyleBuilder.Register(() => _AlignItems switch
        {
            BitAlignment.Start => "align-items:flex-start",
            BitAlignment.End => "align-items:flex-end",
            BitAlignment.Center => "align-items:center",
            BitAlignment.Baseline => "align-items:baseline",
            BitAlignment.Stretch => "align-items:stretch",
            _ => string.Empty
        });

        // Every member means something to align-content, since it is the one alignment that has both a block of
        // rows to park and a set of rows to spread apart.
        StyleBuilder.Register(() => AlignContent switch
        {
            BitAlignment.Start => "align-content:flex-start",
            BitAlignment.End => "align-content:flex-end",
            BitAlignment.Center => "align-content:center",
            BitAlignment.SpaceBetween => "align-content:space-between",
            BitAlignment.SpaceAround => "align-content:space-around",
            BitAlignment.SpaceEvenly => "align-content:space-evenly",
            BitAlignment.Baseline => "align-content:baseline",
            BitAlignment.Stretch => "align-content:stretch",
            _ => string.Empty
        });

        // The layout of an item is a pure function of these four custom properties, which is why they live on the
        // container: they are inherited by every item of this grid and by no item of a nested one, so a grid that
        // changes its column count or its spacing relays its items out without any of them having to be told.
        StyleBuilder.Register(() => $"--bit-grd-span:{Math.Max(0, Span).ToString(CultureInfo.InvariantCulture)}");
        StyleBuilder.Register(() => $"--bit-grd-cols:{Math.Max(1, Columns).ToString(CultureInfo.InvariantCulture)}");
        StyleBuilder.Register(() => $"--bit-grd-cgap:{GetSpacing(HorizontalSpacing)}");
        StyleBuilder.Register(() => $"--bit-grd-rgap:{GetSpacing(VerticalSpacing)}");

        // Only the breakpoints that were asked for are declared. The stylesheet chains every other one to the
        // breakpoint below it, which is what carries a value upwards and makes these mobile first.
        StyleBuilder.Register(() => GetColumnsVar("xs", ColumnsXs));
        StyleBuilder.Register(() => GetColumnsVar("sm", ColumnsSm));
        StyleBuilder.Register(() => GetColumnsVar("md", ColumnsMd));
        StyleBuilder.Register(() => GetColumnsVar("lg", ColumnsLg));
        StyleBuilder.Register(() => GetColumnsVar("xl", ColumnsXl));
        StyleBuilder.Register(() => GetColumnsVar("xxl", ColumnsXxl));
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitGridParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // The grid is the owner of the cascade its items read, and the instance never changes, so the value is
        // fixed: an item is laid out by the custom properties it inherits from the container, never by a
        // parameter of the container it would have to be re-notified about.
        builder.OpenComponent<CascadingValue<BitGrid>>(0);
        builder.AddComponentParameter(1, "Value", this);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(rootBuilder =>
        {
            rootBuilder.OpenElement(0, Element ?? "div");
            rootBuilder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
            rootBuilder.AddAttribute(2, "id", _Id);
            rootBuilder.AddAttribute(3, "aria-label", AriaLabel);
            rootBuilder.AddAttribute(4, "style", StyleBuilder.Value);
            rootBuilder.AddAttribute(5, "class", ClassBuilder.Value);
            rootBuilder.AddAttribute(6, "dir", Dir?.ToString().ToLower());
            rootBuilder.AddElementReferenceCapture(7, v => RootElement = v);
            rootBuilder.AddContent(8, ChildContent);
            rootBuilder.CloseElement();
        }));
        builder.CloseComponent();

        base.BuildRenderTree(builder);
    }



    // Baseline and Stretch distribute nothing, so they are dropped here and picked up by the cross axis below
    // instead of rendering a justify-content the browser throws away.
    private BitAlignment? _JustifyContent => (HorizontalAlign ?? Alignment) switch
    {
        BitAlignment.Baseline or BitAlignment.Stretch => null,
        var alignment => alignment
    };

    // The cross axis takes VerticalAlign, then the two members of HorizontalAlign that only make sense here, and
    // finally the shorthand, so a baseline or stretched grid can be spelled either way.
    private BitAlignment? _AlignItems => (VerticalAlign
                                          ?? (HorizontalAlign is BitAlignment.Baseline or BitAlignment.Stretch ? HorizontalAlign : null)
                                          ?? Alignment) switch
    {
        BitAlignment.SpaceBetween or BitAlignment.SpaceAround or BitAlignment.SpaceEvenly => null,
        var alignment => alignment
    };

    private string GetSpacing(string? axisSpacing)
    {
        var spacing = (axisSpacing.HasValue() ? axisSpacing! : (Spacing.HasValue() ? Spacing : "0px")).Trim();

        // A bare number is not a CSS length, and the width of every item is worked out from the horizontal gap
        // inside a calc() that a unitless value makes invalid - which throws the whole width away and leaves the
        // items at the size of their content. Reading a bare number as pixels is what was meant by it and what
        // keeps the layout standing, and it makes the very common "0" a gap of none rather than a broken grid.
        if (double.TryParse(spacing, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) is false) return spacing;

        // There is no such thing as a negative gap, and letting one through would widen the items past their
        // tracks rather than narrow them, so it is read as no gap at all. The number is written out again
        // rather than echoed, which is what turns the handful of forms CSS does not accept - a leading plus, a
        // trailing decimal point - into the one it does.
        return number > 0 ? $"{number.ToString(CultureInfo.InvariantCulture)}px" : "0px";
    }

    private static string GetColumnsVar(string breakpoint, int? columns)
    {
        if (columns.HasValue is false) return string.Empty;

        return $"--bit-grd-cols-{breakpoint}:{Math.Max(1, columns.Value).ToString(CultureInfo.InvariantCulture)}";
    }
}
