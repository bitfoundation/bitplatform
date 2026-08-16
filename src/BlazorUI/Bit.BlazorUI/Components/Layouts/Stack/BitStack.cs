using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A Stack is a container-type component that abstracts the implementation of a flexbox in order to define the layout of its children components.
/// </summary>
/// <remarks>
/// The stack lays its children out in a single direction - a column by default, a row with <see cref="Horizontal"/> - and puts a
/// <see cref="Gap"/> between them, which is the whole of what most layouts of a page ever need. <see cref="HorizontalAlign"/> and
/// <see cref="VerticalAlign"/> place those children on each of the two axes by name rather than by the flexbox property that happens
/// to reach that axis, so the same parameter means the same thing whichever way the stack faces.
/// <br />
/// Everything else it does is one of four things. It sizes its children: <see cref="FillContent"/> across the axis they are laid out
/// on, and <see cref="GrowContent"/>, <see cref="EqualContent"/> and <see cref="NoShrinkContent"/> along it. It sizes itself:
/// <see cref="AutoWidth"/>, <see cref="FitWidth"/> and the rest replace the full width and height it takes by default. It behaves as
/// a child of another flex container - a stack inside a stack - through <see cref="Grows"/>, <see cref="Basis"/>,
/// <see cref="NoShrink"/>, <see cref="Shrinkable"/>, <see cref="Self"/> and <see cref="Order"/>, which is what makes a nested stack
/// the item of its parent as well as the container of its own children. And it answers to the width of the window through
/// <see cref="HorizontalXs"/> to <see cref="HorizontalXxl"/>, <see cref="GapXs"/> to <see cref="GapXxl"/> and
/// <see cref="WrapXs"/> to <see cref="WrapXxl"/>, which is the column-on-a-phone, row-on-a-desktop layout - alignment,
/// spacing and wrapping included - written as a parameter instead of a media query.
/// </remarks>
public partial class BitStack : BitComponentBase
{
    // The gap of a sized stack is resolved by the stylesheet from the spacing scale of the theme, so it follows
    // the spacing unit and the density of the current theme instead of a hardcoded length.
    private const string SizeVariable = "var(--bit-stc-size)";

    // The gap of a stack that was told nothing about its spacing.
    private const string DefaultGap = "1rem";



    /// <summary>
    /// Gets or sets the cascading parameters for the stack component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple stack components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitStackParams.ParamName)]
    public BitStackParams? CascadingParameters { get; set; }



    /// <summary>
    /// Gets or sets how the rows of a wrapping stack share out the room left over on the cross axis (the CSS align-content).
    /// </summary>
    /// <remarks>
    /// This is the only alignment that acts on the rows themselves rather than on the children within a row, so it says nothing
    /// until <see cref="Wrap"/> has actually produced more than one row and the stack is larger than those rows across.
    /// <br />
    /// Start, Center and End park the block of rows at one end of the stack, the three space distributions spread the rows apart,
    /// and Stretch grows the rows themselves until they fill it. Baseline has no meaning for align-content on a flex container, so
    /// it is dropped rather than written out; lining the children of a row up on their first line of text is what
    /// <see cref="HorizontalAlign"/> and <see cref="VerticalAlign"/> are for.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? AlignContent { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the children of the stack on both axes at once.
    /// </summary>
    /// <remarks>
    /// This is the shorthand of setting <see cref="HorizontalAlign"/> and <see cref="VerticalAlign"/> to the same value, and each
    /// of those takes precedence over it on its own axis.
    /// <br />
    /// The three space distributions of <see cref="BitAlignment"/> only mean something on the axis the children are laid out along,
    /// and Baseline only means something on the axis across it, so those members reach a single axis through this shorthand and are
    /// ignored on the other one. A specific alignment that means nothing on the axis it was given to steps aside for this shorthand
    /// rather than silencing it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Makes the height of the stack follow its content instead of filling its parent.
    /// </summary>
    /// <remarks>
    /// A stack is as tall as whatever contains it by default, which is what a stack that lays a page or a panel out wants and what
    /// a stack of a few rows sitting inside a taller box does not. This is the difference between the two, and
    /// <see cref="FitHeight"/> is the stricter version of it for a stack that is itself a flex child.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool AutoHeight { get; set; }

    /// <summary>
    /// Makes both the width and the height of the stack follow its content instead of filling its parent.
    /// </summary>
    /// <remarks>
    /// This is the shorthand of setting <see cref="AutoWidth"/> and <see cref="AutoHeight"/> together.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool AutoSize { get; set; }

    /// <summary>
    /// Makes the width of the stack follow its content instead of filling its parent.
    /// </summary>
    /// <remarks>
    /// A stack is as wide as whatever contains it by default. This hands the width back to the content, which is what a row of
    /// buttons or a chip that happens to be built as a stack needs so that it does not claim the whole line it sits on.
    /// <see cref="FitWidth"/> is the stricter version of it for a stack that is itself a flex child.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool AutoWidth { get; set; }

    /// <summary>
    /// Gets or sets the size this stack starts from before the leftover space of its own container is shared out (the CSS flex-basis).
    /// </summary>
    /// <remarks>
    /// This describes the stack as a child of another flex container, and it is the size the parent begins with rather than the one
    /// it ends up at: what <see cref="Grow"/> adds and what shrinking takes away are both counted from here. It is a length along the
    /// axis of the parent, so it is a width inside a row and a height inside a column.
    /// <br />
    /// A basis of <c>0</c> is the one every layout reaches for: it takes the content of the stack out of the sum, so several stacks
    /// that also grow end up the same size as each other however much each of them holds, which is how a row of equal columns is
    /// built. The default is <c>auto</c>, which starts from the content.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Basis { get; set; }

    /// <summary>
    /// The content of the stack.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// A stack of things that form a list is more meaningful as a <c>ul</c> whose children are <c>li</c> elements, a stack that is a
    /// region of the page can be a <c>section</c> or a <c>nav</c>, and a stack that lays a group of form controls out can be a
    /// <c>fieldset</c>. The layout itself is unaffected: only the tag name changes, and with it the semantics reported to assistive
    /// technologies - so a landmark element given here is worth pairing with an <see cref="BitComponentBase.AriaLabel"/> that names it.
    /// <br />
    /// A stack rendered as a <c>ul</c>, an <c>ol</c> or a <c>menu</c> is also given an explicit list role, since some browsers drop
    /// the list semantics of those elements as soon as they are laid out as a flex container. An explicit <c>role</c> among the
    /// splatted html attributes still replaces it.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Gives every direct child of the stack an equal share of the axis they are laid out along, whatever each of them holds.
    /// </summary>
    /// <remarks>
    /// The content of the children is taken out of the sum, so a row of three ends up in exact thirds however much longer the label
    /// of one of them is. That is the difference from <see cref="GrowContent"/>, which lays the children out at their natural size
    /// first and only shares out what is left over, leaving a child that holds more larger than one that holds less.
    /// <br />
    /// This is the axis the children are laid out along, and it combines with <see cref="FillContent"/> on the axis across them.
    /// A child that has to keep its own size - an icon or a fixed column - is what <see cref="NoShrinkContent"/> is for instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool EqualContent { get; set; }

    /// <summary>
    /// Expands the direct children of the stack across the axis they are not laid out along.
    /// </summary>
    /// <remarks>
    /// The children of a vertical stack are widened to the full width of the stack and the children of a horizontal one are heightened
    /// to its full height, which is what a menu, a sidebar or a toolbar of equally wide items wants. The size is forced on them, so a
    /// child that states a size of its own across that axis is overridden.
    /// <br />
    /// This is the cross axis alone. Sharing the axis the children are laid out along between them is what
    /// <see cref="GrowContent"/> does, and the two combine.
    /// <br />
    /// A wrapping stack fills the row each child landed on rather than the whole of the stack across it, which is what keeps the
    /// rows of a wrapping stack from each claiming its full height. A stack whose direction changes with the width of the window
    /// (<see cref="HorizontalXs"/> and its siblings) fills the axis of whichever direction currently applies. Both of those are
    /// a stretch rather than a forced size, so unlike the simple case they leave a child that stated a size of its own alone.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FillContent { get; set; }

    /// <summary>
    /// Sets the height of the stack to fit its content.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="AutoHeight"/> this also holds when the stack is a child of another flex container that would otherwise
    /// stretch it, since a fitted size is a definite one and an automatic one is not.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool FitHeight { get; set; }

    /// <summary>
    /// Sets the width and height of the stack to fit its content.
    /// </summary>
    /// <remarks>
    /// This is the shorthand of setting <see cref="FitWidth"/> and <see cref="FitHeight"/> together.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool FitSize { get; set; }

    /// <summary>
    /// Sets the width of the stack to fit its content.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="AutoWidth"/> this also holds when the stack is a child of another flex container that would otherwise
    /// stretch it, since a fitted size is a definite one and an automatic one is not.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack, using any CSS length value.
    /// <br />
    /// The default value is <strong>1rem</strong>.
    /// </summary>
    /// <remarks>
    /// Takes one length for both axes (<c>0.5rem</c>, <c>8px</c>, <c>2%</c>, <c>var(--my-gap)</c>), or two - the vertical one
    /// followed by the horizontal one - for a wrapping stack whose rows want more air than its columns. A fluid length such as
    /// <c>clamp(4px, 1vw, 16px)</c> follows the size of the viewport without a breakpoint of its own.
    /// <br />
    /// The spacing is created with the CSS gap rather than with margins on the children, so it never appears before the first child
    /// or after the last one, it survives a child being hidden, and it leaves the margins of the children to the children.
    /// <br />
    /// <see cref="HorizontalGap"/> and <see cref="VerticalGap"/> each replace it on their own axis, <see cref="GapXs"/> to
    /// <see cref="GapXxl"/> each replace it from their own breakpoint upwards, and it takes precedence over the
    /// <see cref="Size"/> picked from the spacing scale of the theme.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the extra small breakpoint (from 0px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it - so a stack that is tight on a phone and
    /// airy from a tablet up is written as this one plus one more, and nothing in between. <see cref="Gap"/> is the base of that
    /// chain and is what applies at any breakpoint none of these reach, which is what makes them mobile first in the same way
    /// <see cref="HorizontalXs"/> and its siblings are.
    /// <br />
    /// This is the pair of <see cref="HorizontalXs"/> for the spacing rather than the direction, and the two are most often set
    /// together: a stack that folds from a row into a column usually wants a different amount of room between its children there.
    /// A gap that follows the width of the viewport without a breakpoint at all is a fluid length such as
    /// <c>clamp(4px, 1vw, 16px)</c> given to <see cref="Gap"/> instead.
    /// <br />
    /// <see cref="HorizontalGap"/> and <see cref="VerticalGap"/> are written onto the element, so each of them still replaces
    /// whatever the chain resolves to on its own axis at every width.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? GapXs { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the small breakpoint (from 600px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? GapSm { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the medium breakpoint (from 960px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? GapMd { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the large breakpoint (from 1280px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? GapLg { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the extra large breakpoint (from 1920px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? GapXl { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack from the extra extra large breakpoint (from 2560px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? GapXxl { get; set; }

    /// <summary>
    /// Gets or sets how much of the leftover space of its own container this stack takes compared to its siblings (the CSS flex-grow factor).
    /// </summary>
    /// <remarks>
    /// This describes the stack as a child of another flex container rather than as a container itself, so it only says something
    /// inside a parent that has room to share out - most often another stack. Two nested stacks with the factors "1" and "2" split
    /// what their parent did not use one third to two thirds.
    /// <br />
    /// <see cref="Grows"/> is the same thing with the single most common factor, and this takes precedence over it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Grow { get; set; }

    /// <summary>
    /// Lets every direct child of the stack grow into the room the stack has left over along the axis they are laid out on.
    /// </summary>
    /// <remarks>
    /// Each child is given a flex-grow of 1, so what is left over after they have all been laid out at their natural size is shared
    /// out between them equally. A stack that has nothing left over is unchanged, which is why this is the shortest way to a row of
    /// buttons or of tabs that fills its line however many of them there are.
    /// <br />
    /// The children keep the size their content gives them and only the leftover is shared, so a child that holds more stays larger
    /// than one that holds less. <see cref="EqualContent"/> is the version that ends in equal shares instead.
    /// <br />
    /// This is the axis the children are laid out along. Expanding them across the other one is what <see cref="FillContent"/> does,
    /// and the two combine.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool GrowContent { get; set; }

    /// <summary>
    /// Makes the stack take the space its own container has left over (a CSS flex-grow factor of 1).
    /// </summary>
    /// <remarks>
    /// This is <see cref="Grow"/> with the factor every layout reaches for first, and <see cref="Grow"/> replaces it when both are set.
    /// It describes the stack as a child of another flex container, so it needs a parent with room to share out to mean anything.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool Grows { get; set; }

    /// <summary>
    /// Renders the children of the stack side by side in a row instead of stacked in a column.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the direction of the stack at every width. <see cref="HorizontalXs"/> to <see cref="HorizontalXxl"/> replace it from
    /// their own breakpoint upwards, which is what turns a row into a column on a narrow screen.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    /// Gets or sets how the children of the stack are placed on the horizontal axis.
    /// </summary>
    /// <remarks>
    /// In a horizontal stack this is the axis the children are laid out along, so all of Start, Center, End and the three space
    /// distributions apply and Baseline is meaningless. In a vertical stack it is the axis across them, so Start, Center, End,
    /// Baseline and Stretch apply and the space distributions are meaningless. A value that means nothing on the axis it lands on
    /// steps aside for <see cref="Alignment"/>.
    /// <br />
    /// Whether this is the axis the children are laid out along follows the direction the stack currently has, so a stack that
    /// changes direction with the width of the window through <see cref="HorizontalXs"/> to <see cref="HorizontalXxl"/> is realigned
    /// at each of those breakpoints: the same parameter keeps placing the children on the horizontal axis at every width, and which
    /// of its values are meaningful there is decided per breakpoint.
    /// <br />
    /// When neither this nor <see cref="Alignment"/> is set, the children are packed against the start edge of the stack.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? HorizontalAlign { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// This is the room between the columns: between the children of a horizontal stack, and between the columns of a vertical one
    /// once <see cref="Wrap"/> has produced more than one.
    /// <br />
    /// It replaces <see cref="Gap"/> and <see cref="Size"/> on this axis alone, so the other axis keeps whatever they said, and
    /// <see cref="HorizontalGapXs"/> to <see cref="HorizontalGapXxl"/> each replace it from their own breakpoint upwards.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalGap { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra small breakpoint (from 0px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, exactly as <see cref="GapXs"/> and
    /// <see cref="HorizontalXs"/> do, and <see cref="HorizontalGap"/> is the base of that chain. A breakpoint that this chain
    /// never reaches falls back to whatever the <see cref="Gap"/> chain resolves to there - to the second of its two lengths,
    /// where it was given two - so only the axis that needs its own answer has to be written out.
    /// <br />
    /// This is the room between the columns of a wrapping stack that wants a different amount of it than between its rows -
    /// tighter columns on a phone than on a desktop, say - which the single <see cref="Gap"/> chain cannot say on its own.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? HorizontalGapXs { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the small breakpoint (from 600px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? HorizontalGapSm { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the medium breakpoint (from 960px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? HorizontalGapMd { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the large breakpoint (from 1280px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? HorizontalGapLg { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra large breakpoint (from 1920px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? HorizontalGapXl { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the horizontal axis from the extra extra large breakpoint (from 2560px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? HorizontalGapXxl { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// True lays the children out in a row and false in a column, and the value keeps applying to every wider breakpoint until
    /// another one replaces it - so a stack that is a column on a phone and a row from a tablet up is written as this one plus one
    /// more, and nothing in between.
    /// <br />
    /// <see cref="Horizontal"/> is the base of that chain and is what applies at any breakpoint none of these reach.
    /// <see cref="Reversed"/> is not part of it: a reversed stack is reversed at every width.
    /// <br />
    /// <see cref="HorizontalAlign"/> and <see cref="VerticalAlign"/> follow the direction each breakpoint gives, so they keep naming
    /// the same axis of the screen at every width and the stack is realigned along with it - a column centered across the page that
    /// becomes a row spread along it is those two parameters and this one, and nothing else.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? HorizontalXs { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// True lays the children out in a row and false in a column, and the value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? HorizontalSm { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// True lays the children out in a row and false in a column, and the value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? HorizontalMd { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// True lays the children out in a row and false in a column, and the value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? HorizontalLg { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// True lays the children out in a row and false in a column, and the value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? HorizontalXl { get; set; }

    /// <summary>
    /// Gets or sets the direction of the stack from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// True lays the children out in a row and false in a column.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? HorizontalXxl { get; set; }

    /// <summary>
    /// Renders the stack as an inline box, so it flows with the text around it instead of starting a line of its own.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Nothing about the layout of the children changes; only the outside of the stack does. An inline stack is sized by its
    /// content rather than by the box around it - the full width and height a stack takes by default would leave it inline in
    /// name only - which is what a group of chips, of icons or of buttons sitting inside a sentence, a table cell or a label needs.
    /// <br />
    /// A size given through <see cref="BitComponentBase.Style"/> or through the Fit and Auto parameters still wins, so an inline
    /// stack can be pinned to a width without giving up the way it flows.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Inline { get; set; }

    /// <summary>
    /// Keeps the stack at its natural size when its own container runs out of room, instead of letting it be squeezed.
    /// </summary>
    /// <remarks>
    /// A flex child is shrunk below its content before the container is allowed to overflow, which is what silently squashes the
    /// label of a button or the icon at the end of a toolbar. This takes the stack out of that, and the room has to come from its
    /// siblings instead. <see cref="Shrink"/> is the same thing with a factor of its own, and <see cref="NoShrinkContent"/> is this
    /// one applied to the children of the stack rather than to the stack itself.
    /// <br />
    /// The opposite problem - a nested stack that refuses to shrink far enough for the text inside it to be truncated with an
    /// ellipsis - is the automatic minimum size of a flex child, which is what <see cref="Shrinkable"/> undoes.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool NoShrink { get; set; }

    /// <summary>
    /// Keeps every direct child of the stack at its natural size when the stack runs out of room, instead of letting them be squeezed.
    /// </summary>
    /// <remarks>
    /// The children of a flex container are shrunk below their content before the container is allowed to overflow, which is what
    /// silently squashes the labels of a toolbar and the icons at the end of it. This takes all of them out of that at once, so what
    /// no longer fits overflows the stack - or moves onto another row, if <see cref="Wrap"/> was asked for - rather than being
    /// crushed into it.
    /// <br />
    /// <see cref="NoShrink"/> is the same thing for the stack itself within its own container, and a single child that has to keep
    /// its size while the rest give way is a nested stack with that parameter rather than this one.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoShrinkContent { get; set; }

    /// <summary>
    /// Gets or sets the position of the stack among the children of its own container (the CSS order).
    /// </summary>
    /// <remarks>
    /// The children of a flex container are painted from the lowest order to the highest, and those that share one keep the order
    /// they are written in, which defaults to 0 - so a single negative order moves a stack to the front of its siblings and a single
    /// positive one to the back.
    /// <br />
    /// Only the painted order changes. The order the content is read in by a screen reader and reached in by the keyboard stays the
    /// order it is written in, so this is a visual tool and not a way to reorder content.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Order { get; set; }

    /// <summary>
    /// Gets or sets the inner padding of the stack, using any CSS padding value.
    /// </summary>
    /// <remarks>
    /// Takes the CSS shorthand in full, so one length pads every side (<c>1rem</c>), two pad the block and the inline axis
    /// (<c>1rem 2rem</c>), and four pad each side in turn. A fluid length such as <c>clamp(0.5rem, 2vw, 2rem)</c> follows
    /// the size of the viewport, which is what a padding that answers to the width of the window is written as - there is
    /// no per breakpoint chain for it as there is for the direction and the spacing.
    /// <br />
    /// The stack is sized border-box, so padding is taken out of the size the stack already has rather than added to it, and a
    /// padded stack still fits exactly where an unpadded one did.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Padding { get; set; }

    /// <summary>
    /// Renders the children of the stack in the opposite direction (bottom-to-top in a vertical stack, and end-to-start in a horizontal one).
    /// </summary>
    /// <remarks>
    /// The start and end edges of the stack swap over with it, so <see cref="HorizontalAlign"/> and <see cref="VerticalAlign"/> keep
    /// meaning "towards the edge the children begin at" rather than a fixed side of the screen.
    /// <br />
    /// Only the painted order is reversed; the order the children are read in by a screen reader and reached in by the keyboard stays
    /// the order they are written in, so this is a visual tool and not a way to reorder content.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Gets or sets how the stack itself is placed across the axis of its own container (the CSS align-self).
    /// </summary>
    /// <remarks>
    /// This describes the stack as a child of another flex container, and it overrides for this one child whatever that container
    /// aligns its children with - which is how a single item of a row is dropped to the bottom or stretched to the full height while
    /// the rest stay centered.
    /// <br />
    /// Start, Center, End, Baseline and Stretch apply. The three space distributions of <see cref="BitAlignment"/> share room out
    /// between several children, which a single child cannot do, so they are ignored.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Self { get; set; }

    /// <summary>
    /// Gets or sets how much of what its own container is short of this stack gives up compared to its siblings (the CSS flex-shrink factor).
    /// </summary>
    /// <remarks>
    /// This describes the stack as a child of another flex container, and it is the other half of <see cref="Grow"/>: one says how
    /// leftover room is shared out and this one says how a shortage is. Every flex child answers for a factor of 1 by default, so two
    /// nested stacks with the factors "1" and "3" give up a quarter and three quarters of whatever their row is short.
    /// <br />
    /// <see cref="NoShrink"/> is this with the factor a layout reaches for most, zero, and this takes precedence over it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Shrink { get; set; }

    /// <summary>
    /// Lets the stack shrink below the size of what it holds when its own container runs out of room.
    /// </summary>
    /// <remarks>
    /// A flex child is never squeezed below its own content unless it is told it may, which is the automatic minimum size of
    /// the flexbox and the reason a nested stack refuses to shrink far enough for the text inside it to be truncated with an
    /// ellipsis, or for the scrollbar of a pane to appear instead of the page growing. This undoes that on both axes, so what
    /// is inside the stack is free to be clipped, ellipsised or scrolled once the room runs out.
    /// <br />
    /// It is the exact opposite of <see cref="NoShrink"/>, which stops the stack being squeezed at all: this one is about how
    /// far it may be squeezed, not about whether it is. The children of the stack still need whatever makes them give way -
    /// an <c>overflow</c> or a <c>text-overflow</c> of their own - since this only removes the floor the stack itself had.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool Shrinkable { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack, picked from the spacing scale of the theme.
    /// </summary>
    /// <remarks>
    /// This is the way to keep the layouts of an application on the rhythm of its theme: the resulting length follows the spacing
    /// unit and the density of the current theme instead of being hardcoded, and it changes with them.
    /// <br />
    /// <see cref="Gap"/>, <see cref="HorizontalGap"/> and <see cref="VerticalGap"/> are more specific, so each of them takes
    /// precedence over it on the axis it targets.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets how the children of the stack are placed on the vertical axis.
    /// </summary>
    /// <remarks>
    /// In a vertical stack this is the axis the children are laid out along, so all of Start, Center, End and the three space
    /// distributions apply and Baseline is meaningless. In a horizontal stack it is the axis across them, so Start, Center, End,
    /// Baseline and Stretch apply and the space distributions are meaningless. A value that means nothing on the axis it lands on
    /// steps aside for <see cref="Alignment"/>.
    /// <br />
    /// Whether this is the axis the children are laid out along follows the direction the stack currently has, so a stack that
    /// changes direction with the width of the window through <see cref="HorizontalXs"/> to <see cref="HorizontalXxl"/> is realigned
    /// at each of those breakpoints: the same parameter keeps placing the children on the vertical axis at every width, and which of
    /// its values are meaningful there is decided per breakpoint.
    /// <br />
    /// When neither this nor <see cref="Alignment"/> is set, the children are packed against the start edge of the stack.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// This is the room between the rows: between the children of a vertical stack, and between the rows of a horizontal one once
    /// <see cref="Wrap"/> has produced more than one.
    /// <br />
    /// It replaces <see cref="Gap"/> and <see cref="Size"/> on this axis alone, so the other axis keeps whatever they said, and
    /// <see cref="VerticalGapXs"/> to <see cref="VerticalGapXxl"/> each replace it from their own breakpoint upwards.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalGap { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra small breakpoint (from 0px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it, exactly as <see cref="GapXs"/> and
    /// <see cref="HorizontalXs"/> do, and <see cref="VerticalGap"/> is the base of that chain. A breakpoint that this chain
    /// never reaches falls back to whatever the <see cref="Gap"/> chain resolves to there - to the first of its two lengths,
    /// where it was given two - so only the axis that needs its own answer has to be written out.
    /// <br />
    /// This is the room between the rows of a wrapping stack that wants a different amount of it than between its columns,
    /// which the single <see cref="Gap"/> chain cannot say on its own.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? VerticalGapXs { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the small breakpoint (from 600px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? VerticalGapSm { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the medium breakpoint (from 960px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? VerticalGapMd { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the large breakpoint (from 1280px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? VerticalGapLg { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra large breakpoint (from 1920px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? VerticalGapXl { get; set; }

    /// <summary>
    /// Gets or sets the spacing between the children of the stack measured along the vertical axis from the extra extra large breakpoint (from 2560px) upwards, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? VerticalGapXxl { get; set; }

    /// <summary>
    /// Lets the children of the stack move onto more rows when they no longer fit on one.
    /// </summary>
    /// <remarks>
    /// Without this the children of a stack are squeezed to fit rather than moved, which is what a toolbar that must never break
    /// wants and what a row of chips or of filters does not.
    /// <br />
    /// The room between the rows a wrapping stack produces is the <see cref="VerticalGap"/> of a horizontal stack (and the
    /// <see cref="HorizontalGap"/> of a vertical one), and <see cref="AlignContent"/> is what places those rows within the stack.
    /// <br />
    /// This is the answer at every width. <see cref="WrapXs"/> to <see cref="WrapXxl"/> replace it from their own breakpoint
    /// upwards, which is what a toolbar that breaks onto a second row on a phone and never breaks on a desktop is written as.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Wrap { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the extra small breakpoint (from 0px) upwards.
    /// </summary>
    /// <remarks>
    /// True lets the children wrap and false keeps them on one row, and the value keeps applying to every wider breakpoint until
    /// another one replaces it - so a stack that wraps on a phone and never wraps from a tablet up is written as this one plus one
    /// more, and nothing in between.
    /// <br />
    /// <see cref="Wrap"/> is the base of that chain and is what applies at any breakpoint none of these reach.
    /// <see cref="WrapReverse"/> is not part of it: a stack that wraps the other way round does so at every width it wraps at,
    /// exactly as <see cref="Reversed"/> reverses every breakpoint of the direction.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? WrapXs { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the small breakpoint (from 600px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? WrapSm { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the medium breakpoint (from 960px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? WrapMd { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the large breakpoint (from 1280px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? WrapLg { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the extra large breakpoint (from 1920px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? WrapXl { get; set; }

    /// <summary>
    /// Gets or sets whether the children of the stack may move onto more rows, from the extra extra large breakpoint (from 2560px) upwards.
    /// </summary>
    /// <remarks>
    /// The value keeps applying to every wider breakpoint until another one replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool? WrapXxl { get; set; }

    /// <summary>
    /// Lets the children of the stack move onto more rows when they no longer fit on one, with the rows stacked in the opposite direction.
    /// </summary>
    /// <remarks>
    /// The rows are laid out from the far edge of the stack back towards the near one, so the first row ends up last across the
    /// stack. This takes precedence over <see cref="Wrap"/> when both are set, and it is the way every breakpoint of the
    /// <see cref="WrapXs"/> chain that wraps at all wraps.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool WrapReverse { get; set; }



    protected override string RootElementClass => "bit-stc";

    protected override void RegisterCssClasses()
    {
        // The fixed pair gives the children the whole of the cross axis of the stack, which is only the same thing as
        // the whole of the room they were given while the stack has one row and knows which of its two axes is the
        // cross one. A wrapping stack fills its rows and a responsive one is not laid out here, so both of them are
        // handed the class that stretches the children into the room they actually have instead.
        ClassBuilder.Register(() => FillContent
                                        ? (IsRowRelativeFill ? "bit-stc-fcr" : (Horizontal ? "bit-stc-fch" : "bit-stc-fcv"))
                                        : string.Empty);

        ClassBuilder.Register(() => GrowContent ? "bit-stc-grc" : string.Empty);

        ClassBuilder.Register(() => EqualContent ? "bit-stc-eqc" : string.Empty);

        ClassBuilder.Register(() => NoShrinkContent ? "bit-stc-nsc" : string.Empty);

        // The full width and height a stack takes by default would leave an inline one inline in name only, so the
        // sizing is handed back to the content. It is a class rather than an inline style so that an explicit size,
        // whether it comes from Style or from the Fit and Auto parameters, still outranks it.
        ClassBuilder.Register(() => Inline ? "bit-stc-inl" : string.Empty);

        // The class is what hands the direction over to the stylesheet, which is the only place a media query can
        // reach it. A stack that never changes direction keeps its direction inline and never carries this.
        ClassBuilder.Register(() => IsResponsive ? "bit-stc-rsp" : string.Empty);

        // The same for the spacing, which is a chain of its own: a stack may change its gap with the width of the
        // window without changing its direction, and the other way round.
        ClassBuilder.Register(() => IsResponsiveGap ? "bit-stc-rsg" : string.Empty);

        // And one more chain per axis, for the stack that wants a different amount of room between its columns
        // than between its rows at some width. Each of them is only handed out when it was actually asked for,
        // so an axis that never changes keeps whatever the chain above resolves to at every width.
        ClassBuilder.Register(() => IsResponsiveHorizontalGap ? "bit-stc-rshg" : string.Empty);

        ClassBuilder.Register(() => IsResponsiveVerticalGap ? "bit-stc-rsvg" : string.Empty);

        // And one more chain for the stack that only breaks onto a second row at some widths. It is a chain of its
        // own for the same reason all of the others are: whether a stack wraps and which way it faces are two
        // separate questions, and either of them may answer to the width of the window without the other doing so.
        ClassBuilder.Register(() => IsResponsiveWrap ? "bit-stc-rswrp" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-stc-sm",
            BitSize.Medium => "bit-stc-md",
            BitSize.Large => "bit-stc-lg",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Inline ? "display:inline-flex" : "display:flex"); // to preserve display so it can't be overridden easily.

        // The direction of a stack that never changes it is written straight onto the element, where nothing in a
        // stylesheet can reach it. A responsive one hands the same value over as the base of the chain below instead,
        // since an inline flex-direction would outrank every media query that is the whole point of the feature.
        StyleBuilder.Register(() => IsResponsive ? string.Empty : $"flex-direction:{GetDirection(Horizontal)}");
        StyleBuilder.Register(() => IsResponsive ? $"--bit-stc-dir:{GetDirection(Horizontal)}" : string.Empty);

        // Only the breakpoints that were asked for are declared. The stylesheet chains every other one to the
        // breakpoint below it, which is what carries a value upwards and makes these mobile first.
        StyleBuilder.Register(() => GetDirectionVar("xs", HorizontalXs));
        StyleBuilder.Register(() => GetDirectionVar("sm", HorizontalSm));
        StyleBuilder.Register(() => GetDirectionVar("md", HorizontalMd));
        StyleBuilder.Register(() => GetDirectionVar("lg", HorizontalLg));
        StyleBuilder.Register(() => GetDirectionVar("xl", HorizontalXl));
        StyleBuilder.Register(() => GetDirectionVar("xxl", HorizontalXxl));

        // The gap of a stack that keeps the same spacing at every width is written straight onto the element. One
        // that changes it hands the same value over as the base of the chain below instead, since an inline gap
        // would outrank every media query that is the whole point of the feature.
        StyleBuilder.Register(() => IsResponsiveGap ? $"--bit-stc-gap:{ResolvedGap}" : $"gap:{ResolvedGap}");

        // Only the breakpoints that were asked for are declared, exactly as the direction does it: the stylesheet
        // chains every other one to the breakpoint below it, which is what carries a value upwards.
        StyleBuilder.Register(() => GetGapVar("xs", GapXs));
        StyleBuilder.Register(() => GetGapVar("sm", GapSm));
        StyleBuilder.Register(() => GetGapVar("md", GapMd));
        StyleBuilder.Register(() => GetGapVar("lg", GapLg));
        StyleBuilder.Register(() => GetGapVar("xl", GapXl));
        StyleBuilder.Register(() => GetGapVar("xxl", GapXxl));

        // The two axes after the shorthand, so a per axis gap overrides the one that covers both - and, being
        // written onto the element, it also overrides whatever the chain above resolves to at any width. An axis
        // that changes its own spacing with the width of the window hands the same value over as the base of the
        // chain below instead, for the same reason the two above do.
        StyleBuilder.Register(() => IsResponsiveHorizontalGap
                                        ? $"--bit-stc-hgap:{GetAxisGap(Column, HorizontalGap, ResolvedGap)}"
                                        : (HorizontalGap.HasValue() ? $"column-gap:{HorizontalGap}" : string.Empty));

        StyleBuilder.Register(() => IsResponsiveVerticalGap
                                        ? $"--bit-stc-vgap:{GetAxisGap(Row, VerticalGap, ResolvedGap)}"
                                        : (VerticalGap.HasValue() ? $"row-gap:{VerticalGap}" : string.Empty));

        // And the breakpoints of each of the two, declared exactly as the ones of the shorthand are. A breakpoint
        // the axis itself says nothing about but the shorthand does is still declared here, out of the half of the
        // shorthand that belongs to this axis: a chain that skipped it would carry the older value of the axis
        // straight past the width the shorthand changed at.
        StyleBuilder.Register(() => GetAxisGapVar("h", "xs", HorizontalGapXs, GapXs));
        StyleBuilder.Register(() => GetAxisGapVar("h", "sm", HorizontalGapSm, GapSm));
        StyleBuilder.Register(() => GetAxisGapVar("h", "md", HorizontalGapMd, GapMd));
        StyleBuilder.Register(() => GetAxisGapVar("h", "lg", HorizontalGapLg, GapLg));
        StyleBuilder.Register(() => GetAxisGapVar("h", "xl", HorizontalGapXl, GapXl));
        StyleBuilder.Register(() => GetAxisGapVar("h", "xxl", HorizontalGapXxl, GapXxl));

        StyleBuilder.Register(() => GetAxisGapVar("v", "xs", VerticalGapXs, GapXs));
        StyleBuilder.Register(() => GetAxisGapVar("v", "sm", VerticalGapSm, GapSm));
        StyleBuilder.Register(() => GetAxisGapVar("v", "md", VerticalGapMd, GapMd));
        StyleBuilder.Register(() => GetAxisGapVar("v", "lg", VerticalGapLg, GapLg));
        StyleBuilder.Register(() => GetAxisGapVar("v", "xl", VerticalGapXl, GapXl));
        StyleBuilder.Register(() => GetAxisGapVar("v", "xxl", VerticalGapXxl, GapXxl));

        // The two alignments of a stack that never changes direction are written straight onto the element. A
        // responsive one hands over what each breakpoint resolves to instead, since an inline align-items would
        // outrank the media queries - and since which of the two axes each of them reaches, and which of their
        // values mean anything on it, is a different answer at every breakpoint the direction changes at.
        StyleBuilder.Register(() => IsResponsive ? string.Empty : GetAlignment("align-items", _AlignItems));
        StyleBuilder.Register(() => IsResponsive ? string.Empty : GetAlignment("justify-content", _JustifyContent));

        StyleBuilder.Register(() => IsResponsive ? GetAlignmentVars(string.Empty, Horizontal) : string.Empty);
        StyleBuilder.Register(() => IsResponsive ? GetAlignmentVars("xs", HorizontalXs) : string.Empty);
        StyleBuilder.Register(() => IsResponsive ? GetAlignmentVars("sm", HorizontalSm) : string.Empty);
        StyleBuilder.Register(() => IsResponsive ? GetAlignmentVars("md", HorizontalMd) : string.Empty);
        StyleBuilder.Register(() => IsResponsive ? GetAlignmentVars("lg", HorizontalLg) : string.Empty);
        StyleBuilder.Register(() => IsResponsive ? GetAlignmentVars("xl", HorizontalXl) : string.Empty);
        StyleBuilder.Register(() => IsResponsive ? GetAlignmentVars("xxl", HorizontalXxl) : string.Empty);

        // align-content places the rows of a wrapping stack across it, which is the same axis whichever way the
        // stack faces, so unlike the two above it is never resolved per breakpoint.
        StyleBuilder.Register(() => GetAlignment("align-content", _AlignContent));

        StyleBuilder.Register(() => (Grow.HasValue() || Grows) ? $"flex-grow:{(Grow.HasValue() ? Grow : "1")}" : string.Empty);

        StyleBuilder.Register(() => (Shrink.HasValue() || NoShrink) ? $"flex-shrink:{(Shrink.HasValue() ? Shrink : "0")}" : string.Empty);

        StyleBuilder.Register(() => Basis.HasValue() ? $"flex-basis:{Basis}" : string.Empty);

        // The automatic minimum size of a flex child is a floor under both axes at once, so undoing it is too.
        StyleBuilder.Register(() => Shrinkable ? "min-width:0;min-height:0" : string.Empty);

        StyleBuilder.Register(() => GetAlignment("align-self", _AlignSelf));

        StyleBuilder.Register(() => Order.HasValue ? $"order:{Order.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty);

        StyleBuilder.Register(() => Padding.HasValue() ? $"padding:{Padding}" : string.Empty);

        // The wrapping of a stack that never changes it is written straight onto the element. One that only breaks
        // onto a second row at some widths hands the same answer over as the base of the chain below instead, since
        // an inline flex-wrap would outrank every media query that is the whole point of the feature.
        StyleBuilder.Register(() => IsResponsiveWrap
                                        ? $"--bit-stc-wrap:{GetWrap(Wrap || WrapReverse)}"
                                        : (WrapReverse ? "flex-wrap:wrap-reverse" : (Wrap ? "flex-wrap:wrap" : string.Empty)));

        // Only the breakpoints that were asked for are declared, exactly as the direction and the gap do it.
        StyleBuilder.Register(() => GetWrapVar("xs", WrapXs));
        StyleBuilder.Register(() => GetWrapVar("sm", WrapSm));
        StyleBuilder.Register(() => GetWrapVar("md", WrapMd));
        StyleBuilder.Register(() => GetWrapVar("lg", WrapLg));
        StyleBuilder.Register(() => GetWrapVar("xl", WrapXl));
        StyleBuilder.Register(() => GetWrapVar("xxl", WrapXxl));

        StyleBuilder.Register(() => (AutoSize || AutoWidth) ? "width:auto" : string.Empty);
        StyleBuilder.Register(() => (AutoSize || AutoHeight) ? "height:auto" : string.Empty);

        StyleBuilder.Register(() => (FitSize || FitWidth) ? "width:fit-content" : string.Empty);
        StyleBuilder.Register(() => (FitSize || FitHeight) ? "height:fit-content" : string.Empty);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitStackParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? "div");
        // The list role is written before the splatted attributes, so a role given through HtmlAttributes still
        // replaces it - a stack rendered as a "ul" of tabs is a tablist and not a list.
        if (IsListElement)
        {
            builder.AddAttribute(1, "role", "list");
        }
        builder.AddMultipleAttributes(2, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(3, "id", _Id);
        // A null value here is not the same as nothing at all: the builder still records the name and drops the
        // attribute of the same name that came out of HtmlAttributes, so an unset parameter is only added when the
        // parameter itself carries a value and the splatted one is left alone otherwise.
        if (AriaLabel is not null)
        {
            builder.AddAttribute(4, "aria-label", AriaLabel);
        }
        if (TabIndex is not null)
        {
            builder.AddAttribute(5, "tabindex", TabIndex);
        }
        builder.AddAttribute(6, "style", StyleBuilder.Value);
        builder.AddAttribute(7, "class", ClassBuilder.Value);
        if (Dir is not null)
        {
            builder.AddAttribute(8, "dir", Dir.Value.ToString().ToLowerInvariant());
        }
        builder.AddElementReferenceCapture(9, v => RootElement = v);
        builder.AddContent(10, ChildContent);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }



    // A stack only needs the stylesheet to own its direction when that direction is not the same at every width.
    private bool IsResponsive => HorizontalXs.HasValue || HorizontalSm.HasValue || HorizontalMd.HasValue
                              || HorizontalLg.HasValue || HorizontalXl.HasValue || HorizontalXxl.HasValue;

    // And the stylesheet only needs to own the wrapping when the stack does not answer the same way at every width.
    private bool IsResponsiveWrap => WrapXs.HasValue || WrapSm.HasValue || WrapMd.HasValue
                                  || WrapLg.HasValue || WrapXl.HasValue || WrapXxl.HasValue;

    // FillContent is the whole of the cross axis of the stack in the simple case, and the whole of the row a child
    // landed on as soon as there is more than one of them or the cross axis is not known here. A stack that only
    // wraps at some widths may have more than one row at any of them, so it is handed the same class.
    private bool IsRowRelativeFill => IsResponsive || Wrap || WrapReverse || IsResponsiveWrap;

    // And the stylesheet only needs to own the gap when the spacing is not the same at every width either. A per
    // axis chain drags the shorthand into the stylesheet along with it: an inline gap is an inline style, which
    // no rule of a stylesheet can override, so an axis left inline there would win at every width.
    private bool IsResponsiveGap => GapXs.HasValue() || GapSm.HasValue() || GapMd.HasValue()
                                 || GapLg.HasValue() || GapXl.HasValue() || GapXxl.HasValue()
                                 || IsResponsiveHorizontalGap || IsResponsiveVerticalGap;

    private bool IsResponsiveHorizontalGap => HorizontalGapXs.HasValue() || HorizontalGapSm.HasValue() || HorizontalGapMd.HasValue()
                                           || HorizontalGapLg.HasValue() || HorizontalGapXl.HasValue() || HorizontalGapXxl.HasValue();

    private bool IsResponsiveVerticalGap => VerticalGapXs.HasValue() || VerticalGapSm.HasValue() || VerticalGapMd.HasValue()
                                         || VerticalGapLg.HasValue() || VerticalGapXl.HasValue() || VerticalGapXxl.HasValue();

    // A stack rendered as one of the list elements is laid out as a flex container, which is enough for some
    // browsers to stop reporting it - and its children - as a list at all, so the role is stated outright.
    private bool IsListElement => Element is not null
                               && (Element.Equals("ul", StringComparison.OrdinalIgnoreCase)
                                || Element.Equals("ol", StringComparison.OrdinalIgnoreCase)
                                || Element.Equals("menu", StringComparison.OrdinalIgnoreCase));

    // The spacing of the stack, from the most specific source to the least: the explicit CSS length of Gap, then the
    // spacing token of Size, then the length a stack that was told nothing falls back to. The two per axis gaps are
    // not part of this: they are written after it and override it one axis at a time.
    private string ResolvedGap => Gap.HasValue() ? Gap!
                                : Size.HasValue ? SizeVariable
                                : DefaultGap;

    // The two halves of the gap shorthand, which takes the row one first and the column one second.
    private const int Row = 0;
    private const int Column = 1;

    // The axis the children are laid out along is reached by justify-content and the one across them by align-items,
    // and which of the two named alignments reaches which of them depends on the way the stack faces. Reversing the
    // stack does not enter into it: the two properties follow the direction, so their start edge moves with it.
    private BitAlignment? _AlignItems => GetAlignItems(Horizontal);

    private BitAlignment? _JustifyContent => GetJustifyContent(Horizontal);

    private BitAlignment? GetAlignItems(bool horizontal) => GetCrossAlignment(horizontal ? VerticalAlign : HorizontalAlign)
                                                         ?? GetCrossAlignment(Alignment);

    private BitAlignment? GetJustifyContent(bool horizontal) => GetMainAlignment(horizontal ? HorizontalAlign : VerticalAlign)
                                                             ?? GetMainAlignment(Alignment);

    // align-content places the rows of a wrapping stack, so every member but Baseline - which a flex container has no
    // baseline behavior for - means something to it.
    private BitAlignment? _AlignContent => AlignContent is BitAlignment.Baseline ? null : AlignContent;

    // align-self places this one stack within its own container, and a single child has no room to distribute.
    private BitAlignment? _AlignSelf => GetCrossAlignment(Self);

    // Sharing room out between the children says nothing about the axis across them, so those three members are
    // dropped here rather than written out as an align-items the browser throws away. Dropping them is also what lets
    // the axis fall through to the Alignment shorthand instead of being silenced by a value that was never about it.
    private static BitAlignment? GetCrossAlignment(BitAlignment? alignment)
    {
        return alignment is BitAlignment.SpaceBetween or BitAlignment.SpaceAround or BitAlignment.SpaceEvenly ? null : alignment;
    }

    // Lining children up on their first line of text says nothing about the axis they are laid out along, so Baseline
    // is dropped here for the same reason, and falls through to the shorthand in the same way.
    private static BitAlignment? GetMainAlignment(BitAlignment? alignment)
    {
        return alignment is BitAlignment.Baseline ? null : alignment;
    }

    private static string GetAlignment(string property, BitAlignment? alignment)
    {
        var value = GetAlignmentValue(alignment);

        return value.HasValue() ? $"{property}:{value}" : string.Empty;
    }

    // The CSS each member of the enum stands for. A value that is not one of them - an out of range integer cast to
    // the enum - is nothing the browser could act on, so it is dropped rather than written out as an invalid
    // declaration that would take the rest of the line down with it.
    private static string? GetAlignmentValue(BitAlignment? alignment) => alignment switch
    {
        BitAlignment.Start => "flex-start",
        BitAlignment.End => "flex-end",
        BitAlignment.Center => "center",
        BitAlignment.SpaceBetween => "space-between",
        BitAlignment.SpaceAround => "space-around",
        BitAlignment.SpaceEvenly => "space-evenly",
        BitAlignment.Baseline => "baseline",
        BitAlignment.Stretch => "stretch",
        _ => null
    };

    // The pair of alignments a single direction of a responsive stack resolves to, as the two custom properties the
    // stylesheet points the element at while that direction applies. A breakpoint that was not asked for declares
    // nothing at all and is chained to the one below it there, exactly as the direction itself is.
    //
    // A breakpoint that was asked for always declares both, because the two are resolved from the direction and a
    // direction that changes can turn an alignment that meant something into one that means nothing - which has to
    // undo whatever the breakpoint below handed up rather than let it through. The initial value of a custom property
    // is the guaranteed-invalid one, so declaring that is what empties the rest of the chain and leaves the stack at
    // the start edge it falls back to.
    private string GetAlignmentVars(string breakpoint, bool? horizontal)
    {
        if (horizontal.HasValue is false) return string.Empty;

        // A stack that was told no alignment at all declares none of this: the chain is then empty from end to
        // end and the stylesheet lays the stack out by the start edge of both axes, which is what it falls back to.
        if (HorizontalAlign.HasValue is false && VerticalAlign.HasValue is false && Alignment.HasValue is false) return string.Empty;

        var suffix = breakpoint.HasValue() ? $"-{breakpoint}" : string.Empty;

        var alignItems = GetAlignmentVar($"--bit-stc-ai{suffix}", GetAlignItems(horizontal.Value));
        var justifyContent = GetAlignmentVar($"--bit-stc-jc{suffix}", GetJustifyContent(horizontal.Value));

        return $"{alignItems};{justifyContent}";
    }

    private static string GetAlignmentVar(string variable, BitAlignment? alignment)
    {
        return $"{variable}:{GetAlignmentValue(alignment) ?? "initial"}";
    }

    private string GetDirection(bool horizontal)
    {
        return $"{(horizontal ? "row" : "column")}{(Reversed ? "-reverse" : string.Empty)}";
    }

    private string GetDirectionVar(string breakpoint, bool? horizontal)
    {
        return horizontal.HasValue ? $"--bit-stc-dir-{breakpoint}:{GetDirection(horizontal.Value)}" : string.Empty;
    }

    // Whether a stack wraps and which way it wraps are two answers to one property, so the way round is the way
    // round at every breakpoint that wraps at all - exactly as a reversed stack is reversed at every width.
    private string GetWrap(bool wrap)
    {
        return wrap ? (WrapReverse ? "wrap-reverse" : "wrap") : "nowrap";
    }

    private string GetWrapVar(string breakpoint, bool? wrap)
    {
        return wrap.HasValue ? $"--bit-stc-wrap-{breakpoint}:{GetWrap(wrap.Value)}" : string.Empty;
    }

    private static string GetGapVar(string breakpoint, string? gap)
    {
        return gap.HasValue() ? $"--bit-stc-gap-{breakpoint}:{gap}" : string.Empty;
    }

    private string GetAxisGapVar(string axis, string breakpoint, string? axisGap, string? gap)
    {
        var horizontal = axis == "h";

        // The shorthand of a breakpoint is restated here for both axes, but only the axis that has a chain of its
        // own reads any of it back, so an axis that was never asked to change declares nothing at all.
        if (horizontal ? IsResponsiveHorizontalGap is false : IsResponsiveVerticalGap is false) return string.Empty;

        var value = GetAxisGap(horizontal ? Column : Row, axisGap, gap);

        return value.HasValue() ? $"--bit-stc-{axis}gap-{breakpoint}:{value}" : string.Empty;
    }

    // What one axis is spaced by at one breakpoint: the length that axis was given there, and otherwise the half
    // of the shorthand of that breakpoint which belongs to it - a shorthand that only names one length says the
    // same thing about both axes.
    private static string? GetAxisGap(int half, string? axisGap, string? gap)
    {
        if (axisGap.HasValue()) return axisGap;

        if (gap.HasValue() is false) return null;

        var halves = SplitGap(gap!);

        return halves.Count > half ? halves[half] : halves[0];
    }

    // The lengths of a gap shorthand, told apart by the whitespace between them. The whitespace inside a function -
    // clamp(4px, 1vw, 16px) or calc(1rem + 2px) - belongs to the one length it is part of, so only the whitespace
    // outside every bracket separates one length from the next.
    private static List<string> SplitGap(string gap)
    {
        List<string> halves = [];
        var depth = 0;
        var start = -1;

        for (var i = 0; i < gap.Length; i++)
        {
            var c = gap[i];

            if (c == '(') depth++;
            else if (c == ')') depth--;

            if (depth == 0 && char.IsWhiteSpace(c))
            {
                if (start >= 0)
                {
                    halves.Add(gap[start..i]);
                    start = -1;
                }
            }
            else if (start < 0)
            {
                start = i;
            }
        }

        if (start >= 0)
        {
            halves.Add(gap[start..]);
        }

        return halves.Count > 0 ? halves : [gap];
    }
}
