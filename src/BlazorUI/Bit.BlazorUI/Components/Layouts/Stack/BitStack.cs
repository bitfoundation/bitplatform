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
/// Everything else it does is one of three things. It sizes itself: <see cref="AutoWidth"/>, <see cref="FitWidth"/> and the rest
/// replace the full width and height it takes by default. It behaves as a child of another flex container - a stack inside a stack -
/// through <see cref="Grows"/>, <see cref="NoShrink"/>, <see cref="Self"/> and <see cref="Order"/>, which is what makes a nested
/// stack the item of its parent as well as the container of its own children. And it answers to the width of the window through
/// <see cref="HorizontalXs"/> to <see cref="HorizontalXxl"/>, which is the column-on-a-phone, row-on-a-desktop layout written as one
/// parameter instead of a media query.
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
    /// </remarks>
    [Parameter] public string? Element { get; set; }

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
    /// A stack whose direction changes with the width of the window (<see cref="HorizontalXs"/> and its siblings) fills the axis of
    /// whichever direction currently applies, without forcing a size over a child that stated one.
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
    /// <see cref="HorizontalGap"/> and <see cref="VerticalGap"/> each replace it on their own axis, and it takes precedence over the
    /// <see cref="Size"/> picked from the spacing scale of the theme.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

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
    /// It replaces <see cref="Gap"/> and <see cref="Size"/> on this axis alone, so the other axis keeps whatever they said.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? HorizontalGap { get; set; }

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
    /// siblings instead.
    /// <br />
    /// The opposite problem - a nested stack that refuses to shrink far enough for the text inside it to be truncated with an
    /// ellipsis - is the automatic minimum size of a flex child, which is undone with a <c>min-width:0</c> through
    /// <see cref="BitComponentBase.Style"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool NoShrink { get; set; }

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
    /// (<c>1rem 2rem</c>), and four pad each side in turn.
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
    /// It replaces <see cref="Gap"/> and <see cref="Size"/> on this axis alone, so the other axis keeps whatever they said.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? VerticalGap { get; set; }

    /// <summary>
    /// Lets the children of the stack move onto more rows when they no longer fit on one.
    /// </summary>
    /// <remarks>
    /// Without this the children of a stack are squeezed to fit rather than moved, which is what a toolbar that must never break
    /// wants and what a row of chips or of filters does not.
    /// <br />
    /// The room between the rows a wrapping stack produces is the <see cref="VerticalGap"/> of a horizontal stack (and the
    /// <see cref="HorizontalGap"/> of a vertical one), and <see cref="AlignContent"/> is what places those rows within the stack.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool Wrap { get; set; }

    /// <summary>
    /// Lets the children of the stack move onto more rows when they no longer fit on one, with the rows stacked in the opposite direction.
    /// </summary>
    /// <remarks>
    /// The rows are laid out from the far edge of the stack back towards the near one, so the first row ends up last across the
    /// stack. This takes precedence over <see cref="Wrap"/> when both are set.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool WrapReverse { get; set; }



    protected override string RootElementClass => "bit-stc";

    protected override void RegisterCssClasses()
    {
        // A stack whose direction answers to the width of the window fills whichever axis currently applies, which is
        // something only the stylesheet can know - so it is handed its own class rather than one of the fixed pair.
        ClassBuilder.Register(() => FillContent
                                        ? (IsResponsive ? "bit-stc-fcr" : (Horizontal ? "bit-stc-fch" : "bit-stc-fcv"))
                                        : string.Empty);

        ClassBuilder.Register(() => GrowContent ? "bit-stc-grc" : string.Empty);

        // The full width and height a stack takes by default would leave an inline one inline in name only, so the
        // sizing is handed back to the content. It is a class rather than an inline style so that an explicit size,
        // whether it comes from Style or from the Fit and Auto parameters, still outranks it.
        ClassBuilder.Register(() => Inline ? "bit-stc-inl" : string.Empty);

        // The class is what hands the direction over to the stylesheet, which is the only place a media query can
        // reach it. A stack that never changes direction keeps its direction inline and never carries this.
        ClassBuilder.Register(() => IsResponsive ? "bit-stc-rsp" : string.Empty);

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

        // The shorthand first and the two axes after it, so a per axis gap overrides the one that covers both.
        StyleBuilder.Register(() => $"gap:{ResolvedGap}");
        StyleBuilder.Register(() => HorizontalGap.HasValue() ? $"column-gap:{HorizontalGap}" : string.Empty);
        StyleBuilder.Register(() => VerticalGap.HasValue() ? $"row-gap:{VerticalGap}" : string.Empty);

        StyleBuilder.Register(() => _AlignItems.HasValue ? $"align-items:{_AlignmentMap[_AlignItems.Value]}" : string.Empty);

        StyleBuilder.Register(() => _JustifyContent.HasValue ? $"justify-content:{_AlignmentMap[_JustifyContent.Value]}" : string.Empty);

        StyleBuilder.Register(() => _AlignContent.HasValue ? $"align-content:{_AlignmentMap[_AlignContent.Value]}" : string.Empty);

        StyleBuilder.Register(() => (Grow.HasValue() || Grows) ? $"flex-grow:{(Grow.HasValue() ? Grow : "1")}" : string.Empty);

        StyleBuilder.Register(() => NoShrink ? "flex-shrink:0" : string.Empty);

        StyleBuilder.Register(() => _AlignSelf.HasValue ? $"align-self:{_AlignmentMap[_AlignSelf.Value]}" : string.Empty);

        StyleBuilder.Register(() => Order.HasValue ? $"order:{Order.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty);

        StyleBuilder.Register(() => Padding.HasValue() ? $"padding:{Padding}" : string.Empty);

        StyleBuilder.Register(() => WrapReverse ? "flex-wrap:wrap-reverse" : (Wrap ? "flex-wrap:wrap" : string.Empty));

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
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "aria-label", AriaLabel);
        builder.AddAttribute(4, "tabindex", TabIndex);
        builder.AddAttribute(5, "style", StyleBuilder.Value);
        builder.AddAttribute(6, "class", ClassBuilder.Value);
        builder.AddAttribute(7, "dir", Dir?.ToString().ToLower());
        builder.AddElementReferenceCapture(8, v => RootElement = v);
        builder.AddContent(9, ChildContent);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }



    // A stack only needs the stylesheet to own its direction when that direction is not the same at every width.
    private bool IsResponsive => HorizontalXs.HasValue || HorizontalSm.HasValue || HorizontalMd.HasValue
                              || HorizontalLg.HasValue || HorizontalXl.HasValue || HorizontalXxl.HasValue;

    // The spacing of the stack, from the most specific source to the least: the explicit CSS length of Gap, then the
    // spacing token of Size, then the length a stack that was told nothing falls back to. The two per axis gaps are
    // not part of this: they are written after it and override it one axis at a time.
    private string ResolvedGap => Gap.HasValue() ? Gap!
                                : Size.HasValue ? SizeVariable
                                : DefaultGap;

    // The axis the children are laid out along is reached by justify-content and the one across them by align-items,
    // and which of the two named alignments reaches which of them depends on the way the stack faces. Reversing the
    // stack does not enter into it: the two properties follow the direction, so their start edge moves with it.
    private BitAlignment? _AlignItems => GetCrossAlignment(Horizontal ? VerticalAlign : HorizontalAlign)
                                      ?? GetCrossAlignment(Alignment);

    private BitAlignment? _JustifyContent => GetMainAlignment(Horizontal ? HorizontalAlign : VerticalAlign)
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

    private string GetDirection(bool horizontal)
    {
        return $"{(horizontal ? "row" : "column")}{(Reversed ? "-reverse" : string.Empty)}";
    }

    private string GetDirectionVar(string breakpoint, bool? horizontal)
    {
        return horizontal.HasValue ? $"--bit-stc-dir-{breakpoint}:{GetDirection(horizontal.Value)}" : string.Empty;
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
