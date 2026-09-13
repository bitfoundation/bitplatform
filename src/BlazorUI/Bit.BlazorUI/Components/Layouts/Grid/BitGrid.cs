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
/// Everything about that layout is responsive: the grid takes a different column count and a different spacing
/// per breakpoint (<see cref="ColumnsXs"/> to <see cref="ColumnsXxl"/>, <see cref="SpacingXs"/> to
/// <see cref="SpacingXxl"/> and the per axis spacings beside them) and every item takes a different span, offset
/// and order per breakpoint. Those values are mobile first - a value set at one breakpoint keeps applying to every wider
/// one until another value replaces it - so a three column desktop layout collapses to a single column phone
/// layout without repeating itself at every size in between. A span of no columns hides an item, which is how
/// part of a layout is dropped at one size and given its columns back at another, and <see cref="Container"/>
/// measures all of those breakpoints against the width of the grid itself rather than the width of the window.
/// <br />
/// The columns can also be left behind entirely: <see cref="MinItemWidth"/> fits as many equal items on a row as
/// a width will allow and lets the rest wrap, which is a card grid that answers to the room it is given instead
/// of to a list of breakpoints, and <see cref="Grow"/> shares out whatever a row did not use between its items.
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
    /// Start, Center and End park the block of rows at one end of the grid, the three space distributions spread
    /// the rows apart, and Stretch grows the rows themselves until they fill the height.
    /// <br />
    /// Baseline is the one member that has nothing dependable to do here: align-content has no baseline behavior
    /// on a flex container, so the declaration is dropped and the rows are left where they were. Aligning
    /// the items of a row on their baselines is what <see cref="VerticalAlign"/>, or <see cref="Alignment"/> as
    /// its shorthand, is for, since those reach align-items instead.
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
    /// Measures the breakpoints of this BitGrid against its own width instead of the width of the viewport.
    /// </summary>
    /// <remarks>
    /// The grid becomes a CSS query container, and every per breakpoint value of the grid and of its items -
    /// the column counts, the spans, the offsets, the orders and the sizing - is answered by how wide the grid
    /// itself is. The breakpoints keep their usual widths, so a grid inside a 700px panel lays itself out the
    /// way the same grid lays itself out in a 700px window, however wide the window actually is.
    /// <br />
    /// This is what a piece of a page that is reused at more than one width - a card that is a full column on
    /// one page and a third of a row on another - needs in order to carry its own layout around with it
    /// instead of being told about every place it is used.
    /// <br />
    /// A query container is also the containing block of the absolutely positioned elements inside it, and its
    /// width can no longer be worked out from its content, so this is asked for rather than assumed.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Container { get; set; }

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
    /// Lets every child of the BitGrid grow into whatever width its row did not use.
    /// </summary>
    /// <remarks>
    /// A full row has nothing left over and is unchanged, so what this really decides is what the last row of a
    /// grid does: its items are widened until they fill it instead of leaving a hole where the missing items
    /// would have been. It is also the shortest way to a row of equal items of no particular width, since items
    /// that all grow from the same base share the row equally.
    /// <br />
    /// The items that are sized to their own content (<see cref="BitGridItem.Auto"/>) are left alone, since
    /// being exactly as wide as their content is the whole of what they were asked for - and an item that is
    /// only sized to its content at some breakpoints is left alone at those breakpoints alone.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Grow { get; set; }

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
    /// Defines the horizontal spacing between the children of the BitGrid from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingXs"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacingXs { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the BitGrid from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingSm"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacingSm { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the BitGrid from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingMd"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacingMd { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the BitGrid from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingLg"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacingLg { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the BitGrid from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingXl"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacingXl { get; set; }

    /// <summary>
    /// Defines the horizontal spacing between the children of the BitGrid from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// It takes precedence over the <see cref="SpacingXxl"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalSpacingXxl { get; set; }

    /// <summary>
    /// Sizes the children of the BitGrid to a width they may not go below, instead of to a number of columns,
    /// and fits as many of them on a row as that width allows.
    /// </summary>
    /// <remarks>
    /// Takes any CSS length (for example <c>16rem</c>, <c>240px</c> or <c>30%</c>). A bare number is read as pixels.
    /// <br />
    /// The items of a row are equal and share the row between them, and the row holds as many of them as fit at
    /// this width before the rest wrap onto the next one. That makes the number of columns a consequence of how
    /// wide the grid is rather than something to be listed breakpoint by breakpoint, which is what a grid of
    /// cards of a known comfortable size wants: the cards decide how many columns there are, and they keep
    /// deciding it inside a panel, a dialog or a page without any of the three being told about the others.
    /// <br />
    /// The last row is shared out in the same way, so a row that ends up holding a single item widens that item
    /// to the whole of it rather than leaving it at the width of one column. A short last row that has to keep
    /// the width of the rows above it is a counted <see cref="Columns"/> layout rather than a fluid one.
    /// <br />
    /// The column tracks are left behind while this is set, so <see cref="Columns"/>, <see cref="Span"/> and the
    /// spans of the items no longer size anything - though the items that state <see cref="BitGridItem.Auto"/>
    /// or <see cref="BitGridItem.Grow"/> for themselves are still sized the way they asked to be, at the
    /// breakpoints they asked for it at. The offsets are still measured in the columns of the grid, since the
    /// room left before an item is not a width of its own.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? MinItemWidth { get; set; }

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
    /// <see cref="BitGridItem.Offset"/> and <see cref="BitGridItem.AutoOffset"/> leave their room on the edge the
    /// row starts at rather than on the side the text runs from, so reversing the grid moves that room across with
    /// the row: an offset indents from the right of a reversed left-to-right grid, which is where its row begins.
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
    /// breakpoint of its own. A bare number is read as pixels, and a length written with a minus is read as no
    /// room at all, whatever unit it carries, since there is no such thing as a gap that pulls the items together.
    /// <br />
    /// <see cref="HorizontalSpacing"/> and <see cref="VerticalSpacing"/> each replace it on their own axis, which
    /// is what a grid of cards that needs more air between its rows than between its columns wants.
    /// <br />
    /// This is the spacing of every breakpoint that is not overridden, and the per breakpoint spacings
    /// (<see cref="SpacingXs"/> to <see cref="SpacingXxl"/>) replace it from their own breakpoint upwards.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string Spacing { get; set; } = "4px";

    /// <summary>
    /// Defines the spacing between the children of the BitGrid on both axes from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, which is how a layout
    /// that is tight on a phone is given room to breathe on a desktop without a fluid length of its own.
    /// <br />
    /// <see cref="HorizontalSpacingXs"/> and <see cref="VerticalSpacingXs"/> each replace it on their own axis.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? SpacingXs { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the BitGrid on both axes from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and
    /// <see cref="HorizontalSpacingSm"/> and <see cref="VerticalSpacingSm"/> each replace it on their own axis.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? SpacingSm { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the BitGrid on both axes from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and
    /// <see cref="HorizontalSpacingMd"/> and <see cref="VerticalSpacingMd"/> each replace it on their own axis.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? SpacingMd { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the BitGrid on both axes from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and
    /// <see cref="HorizontalSpacingLg"/> and <see cref="VerticalSpacingLg"/> each replace it on their own axis.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? SpacingLg { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the BitGrid on both axes from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and
    /// <see cref="HorizontalSpacingXl"/> and <see cref="VerticalSpacingXl"/> each replace it on their own axis.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? SpacingXl { get; set; }

    /// <summary>
    /// Defines the spacing between the children of the BitGrid on both axes from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// <see cref="HorizontalSpacingXxl"/> and <see cref="VerticalSpacingXxl"/> each replace it on their own axis.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? SpacingXxl { get; set; }

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
    /// <br />
    /// Values below 1 are treated as 1. A span of no columns hides the item that asked for it, and a default of
    /// none would be a grid whose every item is hidden, which is not what a default is for - so an item that is
    /// meant to disappear says so with a <see cref="BitGridItem.ColumnSpan"/> of its own.
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

    /// <summary>
    /// Defines the vertical spacing between the rows of the BitGrid from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingXs"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacingXs { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the BitGrid from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingSm"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacingSm { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the BitGrid from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingMd"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacingMd { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the BitGrid from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingLg"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacingLg { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the BitGrid from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, and it takes precedence
    /// over the <see cref="SpacingXl"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacingXl { get; set; }

    /// <summary>
    /// Defines the vertical spacing between the rows of the BitGrid from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// It takes precedence over the <see cref="SpacingXxl"/> of the same breakpoint.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalSpacingXxl { get; set; }



    protected override string RootElementClass => "bit-grd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => NoWrap ? "bit-grd-nwr" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-grd-rev" : string.Empty);

        ClassBuilder.Register(() => Container ? "bit-grd-cnq" : string.Empty);

        ClassBuilder.Register(() => Grow ? "bit-grd-grw" : string.Empty);

        // The class is what turns the minimum width on, so it is only handed out alongside a width to read.
        ClassBuilder.Register(() => MinItemWidth.HasValue() ? "bit-grd-mnw" : string.Empty);
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

        // align-content is the one alignment that has both a block of rows to park and a set of rows to spread
        // apart, so every member but Baseline means something to it. Baseline is written out as it was given
        // rather than swallowed here, and a flex container, which has no baseline behavior for align-content,
        // drops the declaration on its own.
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
        // A default of no columns would leave every item that named no span of its own with no width and no way
        // of saying so, since only an item can ask to be hidden, so the default falls back to a single column.
        StyleBuilder.Register(() => $"--bit-grd-span:{Math.Max(1, Span).ToString(CultureInfo.InvariantCulture)}");
        StyleBuilder.Register(() => $"--bit-grd-cols:{Math.Max(1, Columns).ToString(CultureInfo.InvariantCulture)}");
        StyleBuilder.Register(() => $"--bit-grd-cgap:{GetSpacing(HorizontalSpacing)}");
        StyleBuilder.Register(() => $"--bit-grd-rgap:{GetSpacing(VerticalSpacing)}");

        StyleBuilder.Register(() => MinItemWidth.HasValue() ? $"--bit-grd-mnw:{GetLength(MinItemWidth!)}" : string.Empty);

        // Only the breakpoints that were asked for are declared. The stylesheet chains every other one to the
        // breakpoint below it, which is what carries a value upwards and makes these mobile first.
        StyleBuilder.Register(() => GetColumnsVar("xs", ColumnsXs));
        StyleBuilder.Register(() => GetColumnsVar("sm", ColumnsSm));
        StyleBuilder.Register(() => GetColumnsVar("md", ColumnsMd));
        StyleBuilder.Register(() => GetColumnsVar("lg", ColumnsLg));
        StyleBuilder.Register(() => GetColumnsVar("xl", ColumnsXl));
        StyleBuilder.Register(() => GetColumnsVar("xxl", ColumnsXxl));

        // The same chain for the room between the items. The spacing of one axis outranks the spacing of both
        // axes at the same breakpoint, exactly as it does at the base of the chain.
        StyleBuilder.Register(() => GetSpacingVar("c", "xs", HorizontalSpacingXs, SpacingXs));
        StyleBuilder.Register(() => GetSpacingVar("c", "sm", HorizontalSpacingSm, SpacingSm));
        StyleBuilder.Register(() => GetSpacingVar("c", "md", HorizontalSpacingMd, SpacingMd));
        StyleBuilder.Register(() => GetSpacingVar("c", "lg", HorizontalSpacingLg, SpacingLg));
        StyleBuilder.Register(() => GetSpacingVar("c", "xl", HorizontalSpacingXl, SpacingXl));
        StyleBuilder.Register(() => GetSpacingVar("c", "xxl", HorizontalSpacingXxl, SpacingXxl));

        StyleBuilder.Register(() => GetSpacingVar("r", "xs", VerticalSpacingXs, SpacingXs));
        StyleBuilder.Register(() => GetSpacingVar("r", "sm", VerticalSpacingSm, SpacingSm));
        StyleBuilder.Register(() => GetSpacingVar("r", "md", VerticalSpacingMd, SpacingMd));
        StyleBuilder.Register(() => GetSpacingVar("r", "lg", VerticalSpacingLg, SpacingLg));
        StyleBuilder.Register(() => GetSpacingVar("r", "xl", VerticalSpacingXl, SpacingXl));
        StyleBuilder.Register(() => GetSpacingVar("r", "xxl", VerticalSpacingXxl, SpacingXxl));
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
    // instead of rendering a justify-content the browser throws away. A HorizontalAlign spelled with one of them
    // is not a horizontal value at all, so this axis falls through to the shorthand rather than being silenced
    // by it - the same way the cross axis below falls through to the shorthand for a distribution.
    private BitAlignment? _JustifyContent => ((HorizontalAlign is BitAlignment.Baseline or BitAlignment.Stretch ? null : HorizontalAlign)
                                              ?? Alignment) switch
    {
        BitAlignment.Baseline or BitAlignment.Stretch => null,
        var alignment => alignment
    };

    // The cross axis takes VerticalAlign, then the two members of HorizontalAlign that only make sense here, and
    // finally the shorthand, so a baseline or stretched grid can be spelled either way. A VerticalAlign spelled
    // with one of the three distributions is ignored the way it is documented to be, which means stepping aside
    // for whatever the shorthand had to say about this axis rather than silencing it.
    private BitAlignment? _AlignItems => ((VerticalAlign is BitAlignment.SpaceBetween or BitAlignment.SpaceAround or BitAlignment.SpaceEvenly ? null : VerticalAlign)
                                          ?? (HorizontalAlign is BitAlignment.Baseline or BitAlignment.Stretch ? HorizontalAlign : null)
                                          ?? Alignment) switch
    {
        BitAlignment.SpaceBetween or BitAlignment.SpaceAround or BitAlignment.SpaceEvenly => null,
        var alignment => alignment
    };

    private string GetSpacing(string? axisSpacing)
    {
        return GetLength(axisSpacing.HasValue() ? axisSpacing! : (Spacing.HasValue() ? Spacing : "0px"));
    }

    private static string GetLength(string value)
    {
        var length = value.Trim();

        // There is no such thing as a negative gap or a negative width, and letting one through would widen the
        // items past their tracks rather than narrow them, so a length written with a leading minus is read as
        // none at all - whether it carries a unit or not, since "-1rem" is every bit the negative gap "-16" is.
        // A calc() or a var() that works out negative is left to the browser, as there is nothing to read here.
        if (length.StartsWith('-')) return "0px";

        // A bare number is not a CSS length, and the width of every item is worked out from the horizontal gap
        // inside a calc() that a unitless value makes invalid - which throws the whole width away and leaves the
        // items at the size of their content. Reading a bare number as pixels is what was meant by it and what
        // keeps the layout standing, and it makes the very common "0" a gap of none rather than a broken grid.
        if (double.TryParse(length, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) is false) return length;

        // The number is written out again rather than echoed, which is what turns the handful of forms CSS does
        // not accept - a leading plus, a trailing decimal point - into the one it does.
        return $"{number.ToString(CultureInfo.InvariantCulture)}px";
    }

    private static string GetColumnsVar(string breakpoint, int? columns)
    {
        if (columns.HasValue is false) return string.Empty;

        return $"--bit-grd-cols-{breakpoint}:{Math.Max(1, columns.Value).ToString(CultureInfo.InvariantCulture)}";
    }

    // The spacing of a single axis at a breakpoint, falling back to the spacing of both axes at that same
    // breakpoint. A breakpoint that was told nothing is left undeclared, and the stylesheet carries the value of
    // the breakpoint below it upwards in its place.
    private static string GetSpacingVar(string axis, string breakpoint, string? axisSpacing, string? spacing)
    {
        var value = axisSpacing.HasValue() ? axisSpacing : spacing;

        return value.HasValue() ? $"--bit-grd-{axis}gap-{breakpoint}:{GetLength(value!)}" : string.Empty;
    }
}
