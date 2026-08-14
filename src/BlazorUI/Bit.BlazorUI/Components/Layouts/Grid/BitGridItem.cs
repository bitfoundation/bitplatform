using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// BitGridItem is a cell of a <see cref="BitGrid"/>, holding the content of one area of the layout and stating how
/// many columns of the grid that area covers.
/// </summary>
/// <remarks>
/// An item spans a single column by default (or as many as the <see cref="BitGrid.Span"/> of its grid asks for) and
/// takes a span, an offset and an order of its own per breakpoint, all of them mobile first: a value set at one
/// breakpoint keeps applying to every wider one until another value replaces it.
/// <br />
/// The width of an item is worked out from the column count and the horizontal spacing of the grid it sits in, so
/// the items of a row always line up on the same tracks no matter how the grid is configured. An item can opt out
/// of the tracks entirely and be as wide as its content (<see cref="Auto"/>) or share whatever width is left over
/// with its siblings (<see cref="Grow"/>), and an item that asks for no columns at all is not shown, which is how
/// a piece of a layout is dropped at one breakpoint and given its columns back at another.
/// </remarks>
public partial class BitGridItem : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the grid item component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple grid item components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitGridItemParams.ParamName)]
    public BitGridItemParams? CascadingParameters { get; set; }

    /// <summary>
    /// Gets the <see cref="BitGrid"/> this item belongs to, or null when the item is rendered on its own.
    /// </summary>
    /// <remarks>
    /// This property receives its value from the enclosing grid via Blazor's cascading parameter mechanism.
    /// </remarks>
    [CascadingParameter] public BitGrid? Parent { get; set; }



    /// <summary>
    /// Defines the vertical alignment of this item within its row, overriding the
    /// <see cref="BitGrid.VerticalAlign"/> of the grid for this item alone (the CSS align-self).
    /// </summary>
    /// <remarks>
    /// This is what pulls a single item out of the alignment of its row: the action buttons of a form row that
    /// have to sit on the bottom edge while every other item of the row is aligned to the top.
    /// <br />
    /// The three space distributions of <see cref="BitAlignment"/> have no meaning on this axis and are ignored.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? AlignSelf { get; set; }

    /// <summary>
    /// Sizes this item to its own content instead of to a number of columns.
    /// </summary>
    /// <remarks>
    /// The item takes exactly the width it needs and never grows or shrinks, which is what the label, the icon or
    /// the button of a row wants while the rest of that row is divided into columns. It is capped at the full
    /// width of the grid, so content that is too wide wraps instead of overflowing.
    /// <br />
    /// Takes precedence over <see cref="Grow"/> and over every span of the item.
    /// <br />
    /// This is the base of a mobile first chain like every other layout value of the item: the per breakpoint
    /// <see cref="AutoXs"/> to <see cref="AutoXxl"/> and <see cref="GrowXs"/> to <see cref="GrowXxl"/> replace it
    /// from their own breakpoint upwards, which is how an item is full width on a phone and only as wide as its
    /// content once there is room for the rest of its row beside it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Auto { get; set; }

    /// <summary>
    /// Sizes this item to its own content from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is sized to its content below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoXs { get; set; }

    /// <summary>
    /// Sizes this item to its own content from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is sized to its content below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoSm { get; set; }

    /// <summary>
    /// Sizes this item to its own content from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is sized to its content below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoMd { get; set; }

    /// <summary>
    /// Sizes this item to its own content from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is sized to its content below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoLg { get; set; }

    /// <summary>
    /// Sizes this item to its own content from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is sized to its content below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoXl { get; set; }

    /// <summary>
    /// Sizes this item to its own content from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// <c>false</c> is how an item that is sized to its content below this breakpoint is given its columns back
    /// from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoXxl { get; set; }

    /// <summary>
    /// Pushes this item to the end edge of its row by turning every column left over before it into the offset.
    /// </summary>
    /// <remarks>
    /// This is the offset an item takes when the number of columns to leave empty is not known in advance: the
    /// item is written where it belongs in the reading order and lands against the end edge of the row anyway,
    /// which is what the action of a header or the total of a summary row wants next to a heading that grows.
    /// <br />
    /// It takes precedence over <see cref="Offset"/> and over every per breakpoint offset of the item, since it
    /// claims the whole of the room those would have measured out.
    /// <br />
    /// Like <see cref="Offset"/>, the room is left on the edge the row starts at, which is the left of a
    /// left-to-right grid, the right of a right-to-left one, and the other end of either once the grid is
    /// <see cref="BitGrid.Reversed"/>.
    /// <br />
    /// This is the base of a mobile first chain like every other layout value of the item: the per breakpoint
    /// <see cref="AutoOffsetXs"/> to <see cref="AutoOffsetXxl"/> replace it from their own breakpoint upwards,
    /// and <c>false</c> hands the room back to <see cref="Offset"/> there.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool AutoOffset { get; set; }

    /// <summary>
    /// Pushes this item to the end edge of its row from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is pushed to the end edge below this breakpoint falls back to <see cref="Offset"/> from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoOffsetXs { get; set; }

    /// <summary>
    /// Pushes this item to the end edge of its row from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is pushed to the end edge below this breakpoint falls back to <see cref="Offset"/> from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoOffsetSm { get; set; }

    /// <summary>
    /// Pushes this item to the end edge of its row from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is pushed to the end edge below this breakpoint falls back to <see cref="Offset"/> from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoOffsetMd { get; set; }

    /// <summary>
    /// Pushes this item to the end edge of its row from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is pushed to the end edge below this breakpoint falls back to <see cref="Offset"/> from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoOffsetLg { get; set; }

    /// <summary>
    /// Pushes this item to the end edge of its row from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that is pushed to the end edge below this breakpoint falls back to <see cref="Offset"/> from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoOffsetXl { get; set; }

    /// <summary>
    /// Pushes this item to the end edge of its row from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// <c>false</c> is how an item that is pushed to the end edge below this breakpoint falls back to
    /// <see cref="Offset"/> from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? AutoOffsetXxl { get; set; }

    /// <summary>
    /// The content of the Grid item.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Number of columns this item should fill.
    /// </summary>
    /// <remarks>
    /// The number is read against the column count of the grid, so a span of 6 in the default 12 column grid is a
    /// half and a span of 6 in an 8 column grid is three quarters.
    /// <br />
    /// This is the span of every breakpoint that is not overridden, and the per breakpoint spans (<see cref="Xs"/>
    /// to <see cref="Xxl"/>) replace it from their own breakpoint upwards.
    /// <br />
    /// A span of 0 is an item that is not shown at all, which is how a piece of a layout is dropped at one size
    /// and given its columns back at another (<c>Xs="0" Md="4"</c> is a whole column that only appears once
    /// there is room for it). The item is still rendered, so a screen reader will not read it either.
    /// <br />
    /// When not set, the item falls back to the <see cref="BitGrid.Span"/> of its grid, which is a single column
    /// unless the grid says otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? ColumnSpan { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// This is the counterpart of <see cref="BitGrid.Element"/>: the items of a grid rendered as a <c>ul</c>
    /// are rendered as <c>li</c>, and the items of a grid rendered as a <c>dl</c> as <c>dt</c> and <c>dd</c>.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Lets this item grow into whatever width is left over in its row, sharing it equally with the other items
    /// that grow.
    /// </summary>
    /// <remarks>
    /// A row of one item that spans a known number of columns next to two growing items is the layout of a
    /// sidebar next to a pair of equal panes, worked out by the browser rather than by a span that would have to
    /// be recalculated whenever the sidebar changes.
    /// <br />
    /// <see cref="Auto"/> takes precedence over it, and both of them take precedence over every span of the item.
    /// <br />
    /// This is the base of a mobile first chain: the per breakpoint <see cref="GrowXs"/> to <see cref="GrowXxl"/>
    /// and <see cref="AutoXs"/> to <see cref="AutoXxl"/> replace it from their own breakpoint upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Grow { get; set; }

    /// <summary>
    /// Lets this item grow into whatever width is left over in its row from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that grows below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? GrowXs { get; set; }

    /// <summary>
    /// Lets this item grow into whatever width is left over in its row from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that grows below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? GrowSm { get; set; }

    /// <summary>
    /// Lets this item grow into whatever width is left over in its row from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that grows below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? GrowMd { get; set; }

    /// <summary>
    /// Lets this item grow into whatever width is left over in its row from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that grows below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? GrowLg { get; set; }

    /// <summary>
    /// Lets this item grow into whatever width is left over in its row from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and <c>false</c> is how
    /// an item that grows below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? GrowXl { get; set; }

    /// <summary>
    /// Lets this item grow into whatever width is left over in its row from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// <c>false</c> is how an item that grows below this breakpoint is given its columns back from it upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool? GrowXxl { get; set; }

    /// <summary>
    /// Number of columns to leave empty before this item.
    /// </summary>
    /// <remarks>
    /// The gap is added to the start edge of the item and is measured in the columns of the grid, so an offset of
    /// 2 pushes the item exactly as far as two items of a single span would, gaps included. This is how an item is
    /// indented, or how a row that holds a single item is pushed away from the edge of the grid.
    /// <br />
    /// This is the offset of every breakpoint that is not overridden, and the per breakpoint offsets
    /// (<see cref="OffsetXs"/> to <see cref="OffsetXxl"/>) replace it from their own breakpoint upwards.
    /// <br />
    /// The empty room is left on the edge the row starts at, so it indents from the left of a left-to-right
    /// grid, from the right of a right-to-left one, and from the other end of either once the grid is
    /// <see cref="BitGrid.Reversed"/> and lays its row out backwards. Values below 0 are treated as 0.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Offset { get; set; }

    /// <summary>
    /// Number of columns to leave empty before this item in the extra small breakpoint (from 0px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OffsetXs { get; set; }

    /// <summary>
    /// Number of columns to leave empty before this item in the small breakpoint (from 600px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OffsetSm { get; set; }

    /// <summary>
    /// Number of columns to leave empty before this item in the medium breakpoint (from 960px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OffsetMd { get; set; }

    /// <summary>
    /// Number of columns to leave empty before this item in the large breakpoint (from 1280px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OffsetLg { get; set; }

    /// <summary>
    /// Number of columns to leave empty before this item in the extra large breakpoint (from 1920px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OffsetXl { get; set; }

    /// <summary>
    /// Number of columns to leave empty before this item in the extra extra large breakpoint (from 2560px).
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OffsetXxl { get; set; }

    /// <summary>
    /// Defines the position of this item among its siblings, regardless of where it is written.
    /// </summary>
    /// <remarks>
    /// Items are painted from the lowest order to the highest, and the ones that share an order keep the order
    /// they are written in. The value can be negative, which is how a single item is moved ahead of a group that
    /// left its order alone.
    /// <br />
    /// This is the order of every breakpoint that is not overridden, and the per breakpoint orders
    /// (<see cref="OrderXs"/> to <see cref="OrderXxl"/>) replace it from their own breakpoint upwards, which is
    /// how a sidebar written after the content of a page is moved above it on a narrow screen.
    /// <br />
    /// Only the painted order changes; the order the item is read in by a screen reader and reached in by the
    /// keyboard stays the order it is written in, so the two must not be allowed to drift apart in a way that
    /// makes the page harder to follow.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Order { get; set; }

    /// <summary>
    /// Defines the position of this item among its siblings in the extra small breakpoint (from 0px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OrderXs { get; set; }

    /// <summary>
    /// Defines the position of this item among its siblings in the small breakpoint (from 600px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OrderSm { get; set; }

    /// <summary>
    /// Defines the position of this item among its siblings in the medium breakpoint (from 960px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OrderMd { get; set; }

    /// <summary>
    /// Defines the position of this item among its siblings in the large breakpoint (from 1280px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OrderLg { get; set; }

    /// <summary>
    /// Defines the position of this item among its siblings in the extra large breakpoint (from 1920px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OrderXl { get; set; }

    /// <summary>
    /// Defines the position of this item among its siblings in the extra extra large breakpoint (from 2560px).
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? OrderXxl { get; set; }

    /// <summary>
    /// Number of columns this item should fill in the extra small breakpoint (from 0px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item
    /// from this breakpoint upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Xs { get; set; }

    /// <summary>
    /// Number of columns this item should fill in the small breakpoint (from 600px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item
    /// from this breakpoint upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Sm { get; set; }

    /// <summary>
    /// Number of columns this item should fill in the medium breakpoint (from 960px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item
    /// from this breakpoint upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Md { get; set; }

    /// <summary>
    /// Number of columns this item should fill in the large breakpoint (from 1280px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item
    /// from this breakpoint upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Lg { get; set; }

    /// <summary>
    /// Number of columns this item should fill in the extra large breakpoint (from 1920px).
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and 0 hides the item
    /// from this breakpoint upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Xl { get; set; }

    /// <summary>
    /// Number of columns this item should fill in the extra extra large breakpoint (from 2560px).
    /// </summary>
    /// <remarks>
    /// A value of 0 hides the item from this breakpoint upwards.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? Xxl { get; set; }



    protected override string RootElementClass => "bit-grd-itm";

    protected override void RegisterCssClasses()
    {
        // Sizing to the content and growing into the free width are two ways of leaving the column tracks, so only
        // one of them can win. The more specific of the two is the one that names an exact width.
        //
        // An item that keeps the same way of taking up room at every size says so once, which is the pair of
        // classes that sit at the end of the stylesheet and beat every breakpoint at once. An item that changes
        // its mind as the screen widens has to say which of the three it follows at each breakpoint instead,
        // since a breakpoint can only be talked out of the one below it by naming its own.
        ClassBuilder.Register(() => _HasResponsiveSizing
                                    ? _SizingClasses
                                    : (Auto ? "bit-grd-itm-atc" : (Grow ? "bit-grd-itm-grw" : string.Empty)));

        // The offset and the order are only declared for the items that asked for them, so an item that asked for
        // neither keeps its own margin and its own order, whatever a stylesheet of the application gives it.
        // Claiming the whole of the free room and measuring a number of columns out of it are two ways of
        // writing the same margin, so only one of them is ever handed out.
        //
        // An item that claims the free room at every size says so once, which is the class that names the margin
        // outright. An item that only claims it at some of them carries the counted margin and overwrites it with
        // the free room at each breakpoint that asked for it, since a breakpoint can only be talked out of the
        // one below it by naming its own.
        ClassBuilder.Register(() => _OffsetClasses);

        ClassBuilder.Register(() => _HasOrder ? "bit-grd-itm-ord" : string.Empty);

        // A span of no columns is an item that is not shown. An item whose span never changes is either shown
        // or not at every size, and one whose span changes has to be answered for at each breakpoint, since
        // being hidden is the one thing the breakpoint above does not restate on its own.
        ClassBuilder.Register(() => _HiddenClasses);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => AlignSelf switch
        {
            BitAlignment.Start => "align-self:flex-start",
            BitAlignment.End => "align-self:flex-end",
            BitAlignment.Center => "align-self:center",
            BitAlignment.Baseline => "align-self:baseline",
            BitAlignment.Stretch => "align-self:stretch",
            _ => string.Empty
        });

        // Every one of these is the base of a mobile first chain the stylesheet carries upwards: a breakpoint that
        // is not declared here inherits the value of the breakpoint below it, and the base of the span chain is
        // inherited from the grid itself when the item does not set one.
        StyleBuilder.Register(() => GetVar("span", ColumnSpan));
        StyleBuilder.Register(() => GetVar("xs", Xs));
        StyleBuilder.Register(() => GetVar("sm", Sm));
        StyleBuilder.Register(() => GetVar("md", Md));
        StyleBuilder.Register(() => GetVar("lg", Lg));
        StyleBuilder.Register(() => GetVar("xl", Xl));
        StyleBuilder.Register(() => GetVar("xxl", Xxl));

        StyleBuilder.Register(() => GetVar("off", Offset));
        StyleBuilder.Register(() => GetVar("off-xs", OffsetXs));
        StyleBuilder.Register(() => GetVar("off-sm", OffsetSm));
        StyleBuilder.Register(() => GetVar("off-md", OffsetMd));
        StyleBuilder.Register(() => GetVar("off-lg", OffsetLg));
        StyleBuilder.Register(() => GetVar("off-xl", OffsetXl));
        StyleBuilder.Register(() => GetVar("off-xxl", OffsetXxl));

        // An order can be negative, which is how a single item is moved ahead of the ones that left it alone.
        StyleBuilder.Register(() => GetVar("ord", Order, allowNegative: true));
        StyleBuilder.Register(() => GetVar("ord-xs", OrderXs, allowNegative: true));
        StyleBuilder.Register(() => GetVar("ord-sm", OrderSm, allowNegative: true));
        StyleBuilder.Register(() => GetVar("ord-md", OrderMd, allowNegative: true));
        StyleBuilder.Register(() => GetVar("ord-lg", OrderLg, allowNegative: true));
        StyleBuilder.Register(() => GetVar("ord-xl", OrderXl, allowNegative: true));
        StyleBuilder.Register(() => GetVar("ord-xxl", OrderXxl, allowNegative: true));
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitGridItemParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? "div");
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "aria-label", AriaLabel);
        builder.AddAttribute(4, "style", StyleBuilder.Value);
        builder.AddAttribute(5, "class", ClassBuilder.Value);
        builder.AddAttribute(6, "dir", Dir?.ToString().ToLower());
        builder.AddElementReferenceCapture(7, v => RootElement = v);
        builder.AddContent(8, ChildContent);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }



    // The three ways an item can take up the room of its row: the width of its own content, whatever is left
    // over, and a counted number of the columns of its grid.
    private enum _Sizing { Auto, Grow, Span }

    private bool _HasResponsiveSizing => AutoXs.HasValue || AutoSm.HasValue || AutoMd.HasValue
                                      || AutoLg.HasValue || AutoXl.HasValue || AutoXxl.HasValue
                                      || GrowXs.HasValue || GrowSm.HasValue || GrowMd.HasValue
                                      || GrowLg.HasValue || GrowXl.HasValue || GrowXxl.HasValue;

    private bool _HasResponsiveSpan => Xs.HasValue || Sm.HasValue || Md.HasValue
                                    || Lg.HasValue || Xl.HasValue || Xxl.HasValue;

    // How the item takes up room, and how many columns it asks for, at each of the six breakpoints: both of
    // them carried up from the breakpoint below until a value of its own replaces it. The span is left null
    // where the item never named one, since what it falls back to then belongs to the grid rather than here.
    private (string Breakpoint, _Sizing Sizing, int? Span)[] _Breakpoints
    {
        get
        {
            (string Breakpoint, bool? Auto, bool? Grow, int? Span)[] breakpoints =
            [
                ("xs", AutoXs, GrowXs, Xs),
                ("sm", AutoSm, GrowSm, Sm),
                ("md", AutoMd, GrowMd, Md),
                ("lg", AutoLg, GrowLg, Lg),
                ("xl", AutoXl, GrowXl, Xl),
                ("xxl", AutoXxl, GrowXxl, Xxl)
            ];

            var result = new (string, _Sizing, int?)[breakpoints.Length];
            var sizing = Auto ? _Sizing.Auto : (Grow ? _Sizing.Grow : _Sizing.Span);
            var span = ColumnSpan;

            for (var i = 0; i < breakpoints.Length; i++)
            {
                var (breakpoint, auto, grow, ownSpan) = breakpoints[i];

                // A breakpoint that was told nothing keeps what the one below it was doing, and one that was told
                // to stop doing something falls back to the columns rather than to the other way of leaving them.
                sizing = (auto ?? (sizing is _Sizing.Auto))
                            ? _Sizing.Auto
                            : ((grow ?? (sizing is _Sizing.Grow)) ? _Sizing.Grow : _Sizing.Span);

                span = ownSpan ?? span;

                result[i] = (breakpoint, sizing, span);
            }

            return result;
        }
    }

    // The way the item takes up room at each breakpoint, named as the class the stylesheet reads there alone.
    private string _SizingClasses
    {
        get
        {
            return string.Join(' ', _Breakpoints.Select(b => b.Sizing switch
            {
                _Sizing.Auto => $"bit-grd-itm-atc-{b.Breakpoint}",
                _Sizing.Grow => $"bit-grd-itm-grw-{b.Breakpoint}",
                _ => $"bit-grd-itm-spn-{b.Breakpoint}"
            }));
        }
    }

    // The breakpoints at which the item asks for no columns at all, which is the item asking not to be shown.
    // An item that leaves the tracks behind is never hidden by this: being exactly as wide as its content, or
    // taking whatever the row has left, is a width of its own and has nothing to do with a count of columns.
    private string _HiddenClasses
    {
        get
        {
            // An item whose sizing and span are the same at every size is either shown at all of them or at
            // none of them, and says so once rather than six times.
            if (_HasResponsiveSizing is false && _HasResponsiveSpan is false)
            {
                return (Auto is false && Grow is false && ColumnSpan <= 0) ? "bit-grd-itm-non" : string.Empty;
            }

            return string.Join(' ', _Breakpoints.Where(b => b.Sizing is _Sizing.Span && b.Span <= 0)
                                                .Select(b => $"bit-grd-itm-non-{b.Breakpoint}"));
        }
    }

    private bool _HasOffset => Offset.HasValue
                            || OffsetXs.HasValue || OffsetSm.HasValue || OffsetMd.HasValue
                            || OffsetLg.HasValue || OffsetXl.HasValue || OffsetXxl.HasValue;

    private bool _HasResponsiveAutoOffset => AutoOffsetXs.HasValue || AutoOffsetSm.HasValue || AutoOffsetMd.HasValue
                                          || AutoOffsetLg.HasValue || AutoOffsetXl.HasValue || AutoOffsetXxl.HasValue;

    // Which of the two margins the item is given, and at which breakpoints. An item that asked for neither is
    // given none of them and keeps whatever margin a stylesheet of the application hands it.
    private string _OffsetClasses
    {
        get
        {
            if (_HasResponsiveAutoOffset is false)
            {
                return AutoOffset ? "bit-grd-itm-aof" : (_HasOffset ? "bit-grd-itm-off" : string.Empty);
            }

            var autoOffsets = _AutoOffsetClasses;

            // The margin itself hangs off the counted class, which is what the breakpoints that did not claim
            // the free room are left with, so it is handed out alongside them.
            return autoOffsets.Length > 0
                    ? $"bit-grd-itm-off {autoOffsets}"
                    : (_HasOffset ? "bit-grd-itm-off" : string.Empty);
        }
    }

    // The breakpoints at which the item claims the whole of the room left before it, carried up from the
    // breakpoint below until a value of its own replaces it, and named as the class the stylesheet reads there.
    private string _AutoOffsetClasses
    {
        get
        {
            (string Breakpoint, bool? AutoOffset)[] breakpoints =
            [
                ("xs", AutoOffsetXs),
                ("sm", AutoOffsetSm),
                ("md", AutoOffsetMd),
                ("lg", AutoOffsetLg),
                ("xl", AutoOffsetXl),
                ("xxl", AutoOffsetXxl)
            ];

            var classes = new List<string>(breakpoints.Length);
            var autoOffset = AutoOffset;

            foreach (var (breakpoint, ownAutoOffset) in breakpoints)
            {
                autoOffset = ownAutoOffset ?? autoOffset;

                if (autoOffset)
                {
                    classes.Add($"bit-grd-itm-aof-{breakpoint}");
                }
            }

            return string.Join(' ', classes);
        }
    }

    private bool _HasOrder => Order.HasValue
                           || OrderXs.HasValue || OrderSm.HasValue || OrderMd.HasValue
                           || OrderLg.HasValue || OrderXl.HasValue || OrderXxl.HasValue;

    private static string GetVar(string name, int? value, bool allowNegative = false)
    {
        if (value.HasValue is false) return string.Empty;

        var number = allowNegative ? value.Value : Math.Max(0, value.Value);

        // The invariant culture is what keeps a negative order readable as CSS: the minus sign of the
        // current culture is not always the hyphen the browser is looking for.
        return $"--bit-grd-{name}:{number.ToString(CultureInfo.InvariantCulture)}";
    }
}
