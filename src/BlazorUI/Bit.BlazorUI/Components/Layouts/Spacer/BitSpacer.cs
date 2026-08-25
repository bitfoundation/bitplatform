using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// The purpose of the BitSpacer is to generate space between other components inside a flex container.
/// It either creates a fixed amount of space (in pixels through Width/Height, in any CSS length through Gap,
/// or from the spacing scale of the theme through Size) or a flexible space that absorbs whatever room is
/// left over (optionally in proportion to other spacers through Grow).
/// </summary>
public partial class BitSpacer : BitComponentBase
{
    // The fixed space of a sized spacer is resolved by the stylesheet from the spacing scale of the theme,
    // so it follows the spacing unit and the density of the current theme instead of a hardcoded length.
    private const string SizeVariable = "var(--bit-spc-size)";



    /// <summary>
    /// Gets or sets the cascading parameters for the spacer component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple spacer components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitSpacerParams.ParamName)]
    public BitSpacerParams? CascadingParameters { get; set; }



    /// <summary>
    /// Gets or sets the custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// This is what lets a spacer sit in a container that only accepts specific children, such as an "li" inside a list-based
    /// toolbar or menu, without breaking the markup of that container.
    /// </remarks>
    [Parameter]
    public string? Element { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates, using any CSS length value (for example "1rem", "5%", or "var(--my-gap)").
    /// </summary>
    /// <remarks>
    /// The space is created along the inline (horizontal) axis, or along the block (vertical) axis when <see cref="Vertical"/> is set.
    /// <br />
    /// It takes precedence over <see cref="Width"/>, <see cref="Height"/> and <see cref="Size"/> on the axis it targets, and like them it turns
    /// the spacer from a flexible one into a fixed one, so <see cref="Grow"/> and <see cref="MinGap"/> no longer apply.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// Gets or sets how much of the leftover space of the container this spacer takes compared to its siblings (the CSS flex-grow factor).
    /// <br />
    /// The default value is <strong>1</strong>.
    /// </summary>
    /// <remarks>
    /// Two spacers with the factors "1" and "2" split the leftover space of the container one third to two thirds.
    /// <br />
    /// It is ignored as soon as the spacer is given a fixed size through <see cref="Gap"/>, <see cref="Width"/>, <see cref="Height"/> or <see cref="Size"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Grow { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates along the block (vertical) axis, in pixels.
    /// </summary>
    /// <remarks>
    /// This is the parameter to reach for inside a vertical (column) layout, where the inline-axis <see cref="Width"/> creates no visible space.
    /// <br />
    /// Setting it turns the spacer from a flexible one into a fixed one, so <see cref="Grow"/> and <see cref="MinGap"/> no longer apply.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the smallest amount of space a flexible spacer keeps when the container runs out of room, using any CSS length value.
    /// </summary>
    /// <remarks>
    /// A flexible spacer collapses to nothing once its siblings fill the container. This parameter puts a floor under that,
    /// so the neighbours on either side of the spacer never end up touching.
    /// <br />
    /// It applies to the inline (horizontal) axis, or to the block (vertical) axis when <see cref="Vertical"/> is set.
    /// <br />
    /// It only concerns a flexible spacer, so it is ignored as soon as the spacer is given a fixed size through
    /// <see cref="Gap"/>, <see cref="Width"/>, <see cref="Height"/> or <see cref="Size"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? MinGap { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates, picked from the spacing scale of the theme.
    /// </summary>
    /// <remarks>
    /// This is the way to keep the spacers of an application on the rhythm of its theme: the resulting length follows the
    /// spacing unit and the density of the current theme instead of being hardcoded, and it changes with them.
    /// <br />
    /// The space is created along the inline (horizontal) axis, or along the block (vertical) axis when <see cref="Vertical"/> is set,
    /// and like the other fixed sizes it turns the spacer from a flexible one into a fixed one, so <see cref="Grow"/> and <see cref="MinGap"/> no longer apply.
    /// <br />
    /// <see cref="Gap"/>, <see cref="Width"/> and <see cref="Height"/> are more specific, so they take precedence over it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Gap"/>, <see cref="Size"/> and <see cref="MinGap"/> apply to the block (vertical) axis instead of the inline (horizontal) one.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A flexible spacer does not need this parameter: it always grows along the main axis of its container, whichever that is.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool Vertical { get; set; }

    /// <summary>
    /// Gets or sets the fixed amount of space the spacer generates along the inline (horizontal) axis, in pixels.
    /// </summary>
    /// <remarks>
    /// The space follows the text direction of the spacer, so in a right-to-left context it is created on the right side.
    /// <br />
    /// Setting it turns the spacer from a flexible one into a fixed one, so <see cref="Grow"/> and <see cref="MinGap"/> no longer apply.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Width { get; set; }



    // The fixed space of the spacer per axis, from the most specific source to the least: the explicit CSS length of
    // Gap, then the pixel shorthands, then the spacing token of Size. Gap and Size target the inline axis unless the
    // spacer is marked as Vertical, while Width and Height each own their own axis whichever way the spacer faces.
    private string? InlineGap => (Vertical is false && Gap.HasValue()) ? Gap
                               : Width.HasValue ? $"{Width.Value.ToString(CultureInfo.InvariantCulture)}px"
                               : (Vertical is false && Size.HasValue) ? SizeVariable
                               : null;
    private string? BlockGap => (Vertical && Gap.HasValue()) ? Gap
                              : Height.HasValue ? $"{Height.Value.ToString(CultureInfo.InvariantCulture)}px"
                              : (Vertical && Size.HasValue) ? SizeVariable
                              : null;

    // A spacer that was given a fixed size on either axis is not a flexible one any more, so the parameters
    // that only describe how it flexes (Grow and MinGap) stop applying to it.
    private bool IsFixed => InlineGap.HasValue() || BlockGap.HasValue();



    protected override string RootElementClass => "bit-spc";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-spc-sm",
            BitSize.Medium => "bit-spc-md",
            BitSize.Large => "bit-spc-lg",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        // The space is created with a margin rather than a width so the spacer itself stays a zero-sized box:
        // margins are immune to flex shrinking, which keeps a fixed spacer fixed in a cramped container.
        StyleBuilder.Register(() =>
        {
            var gap = InlineGap;
            return gap.HasValue() ? $"margin-inline-start:{gap}" : string.Empty;
        });

        StyleBuilder.Register(() =>
        {
            var gap = BlockGap;
            return gap.HasValue() ? $"margin-block-start:{gap}" : string.Empty;
        });

        // A spacer that was given a fixed size must not also absorb the leftover space of its container.
        StyleBuilder.Register(() => IsFixed ? string.Empty : $"flex-grow:{(Grow.HasValue() ? Grow : "1")}");

        // The floor only makes sense while the spacer is free to collapse, which a fixed one never is.
        StyleBuilder.Register(() => (MinGap.HasValue() && IsFixed is false)
                                        ? $"min-{(Vertical ? "block" : "inline")}-size:{MinGap}"
                                        : string.Empty);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSpacerParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? "div");

        // The spacer carries no content, so it is hidden from assistive technologies by default. It is added before
        // the splatted attributes so an explicit aria-hidden in HtmlAttributes can still take it back.
        builder.AddAttribute(1, "aria-hidden", AriaLabel.HasValue() ? null : "true");
        builder.AddMultipleAttributes(2, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(3, "id", _Id);
        builder.AddAttribute(4, "aria-label", AriaLabel);
        builder.AddAttribute(5, "style", StyleBuilder.Value);
        builder.AddAttribute(6, "class", ClassBuilder.Value);
        builder.AddAttribute(7, "dir", Dir?.ToString().ToLower());
        builder.AddElementReferenceCapture(8, v => RootElement = v);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
