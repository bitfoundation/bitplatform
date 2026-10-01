using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A Separator is a component that visually separates content into groups.
/// </summary>
/// <remarks>
/// The line runs horizontally across its container unless <see cref="Vertical"/> stands it up, and any
/// <see cref="ChildContent"/> - a label, an icon - sits in the line where <see cref="AlignContent"/> puts
/// it, nudged from the edge by <see cref="ContentOffset"/>. The rule is then drawn as the two segments
/// flanking that content rather than as one line behind it, so the content needs no background of its own
/// to be readable and the separator stays right over a gradient, a picture or any surface tier.
/// The line itself is drawn by the theme and restyled through <see cref="LineStyle"/>, <see cref="Size"/>,
/// <see cref="Thickness"/> and <see cref="Color"/>, while <see cref="Background"/> and <see cref="Border"/>
/// keep it on the neutral surface tiers and <see cref="Inset"/> holds it off the ends of its container.
/// The public <c>--bit-Separator-*</c> CSS variables re-skin what the theme would otherwise decide, and a
/// <see cref="BitParams"/> ancestor sets the defaults of every separator under it.
/// <br />
/// To assistive technologies the root reports itself as a separator, named by its content or by an
/// <see cref="BitComponentBase.AriaLabel"/>; a separator that is only visual sugar opts out of being
/// announced at all through <see cref="Decorative"/>.
/// </remarks>
public partial class BitSeparator : BitComponentBase
{
    private const string DefaultElement = "div";



    private string _contentId => $"{_Id}-cnt";

    private string? _role => Decorative ? "none" : "separator";

    // aria-orientation implicitly defaults to horizontal on the separator role, so only vertical needs saying.
    private string? _ariaOrientation => Decorative is false && Vertical ? "vertical" : null;



    /// <summary>
    /// Gets or sets the cascading parameters for the separator component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple separator components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitSeparatorParams.ParamName)]
    public BitSeparatorParams? CascadingParameters { get; set; }



    /// <summary>
    /// Where the content should be aligned in the separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSeparatorAlignContent? AlignContent { get; set; }

    /// <summary>
    /// Renders the separator with auto width or height.
    /// </summary>
    /// <remarks>
    /// A horizontal separator is as wide as its container and a vertical one as tall - a vertical separator
    /// even stretches to the height of the flex row it stands in. This lets it size to its content instead,
    /// which is what a separator that should follow its flex container's align-items wants.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool AutoSize { get; set; }

    /// <summary>
    /// The color kind of the background of the content of the separator.
    /// </summary>
    /// <remarks>
    /// The rule is drawn as two segments flanking the content, so the content needs no background to be
    /// readable; this paints one behind it anyway, for content that should read as a chip on a surface of
    /// its own. It defaults to transparent, which lets whatever the separator is drawn on show through.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the line of the separator, out of the neutral border tiers of the theme.
    /// </summary>
    /// <remarks>
    /// This picks between the neutral strengths of the theme; <see cref="Color"/> paints the line in one of
    /// the theme's roles instead, and wins where both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// The content of the Separator, it can be any custom tag or text.
    /// </summary>
    /// <remarks>
    /// It sits between the two segments of the line, where <see cref="AlignContent"/> puts it, and it also
    /// names the separator to assistive technologies - the children of a separator are presentational, so
    /// the name is wired through aria-labelledby rather than read out of the line. For the same reason
    /// nothing focusable belongs in here: a link or a button inside a separator is announced by neither its
    /// own role nor its own name. Content that has to be interactive belongs beside the separator, or in a
    /// <see cref="Decorative"/> one, which is not a separator to assistive technologies at all.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSeparatorClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the line of the separator.
    /// </summary>
    /// <remarks>
    /// Setting it paints the line in one of the roles of the theme instead of in the neutral border colors,
    /// so every preset and both schemes re-skin it. It wins over <see cref="Border"/>, which picks between
    /// the neutral tiers.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The offset of the content from the edge of the line it is aligned to, as any CSS length.
    /// </summary>
    /// <remarks>
    /// It only means anything while <see cref="AlignContent"/> is Start or End - centered content has no
    /// edge to be offset from. It is the length of the short segment of the line before the content, so a
    /// percentage measures against the length of the separator whichever way it runs. It is direction-aware
    /// on a horizontal separator, and on a vertical one it pushes the content down from the top or up from
    /// the bottom.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? ContentOffset { get; set; }

    /// <summary>
    /// Removes the separator from the accessibility tree, for a separator that is purely visual.
    /// </summary>
    /// <remarks>
    /// A page can carry many rules that mean nothing - each announced as "separator" is noise to a screen
    /// reader. A decorative separator keeps its looks and reports itself as none; one that genuinely splits
    /// content into groups is left announced, and content given to a decorative separator is read as plain
    /// text in the flow rather than as the name of anything.
    /// </remarks>
    [Parameter] public bool Decorative { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// A list or a menu may only hold its items, so the separator between two of them has to be one: an "li"
    /// is what keeps a <c>ul</c> valid, and it still reports itself as a separator rather than as an item, so
    /// a screen reader neither counts it nor announces it as one. The value is used as written and only while
    /// it is a name a tag can have and one that can hold content; anything else - a void element such as "hr"
    /// included - falls back to "div".
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Holds the separator off the ends of its container: one CSS length for both ends, or two for the start
    /// and the end.
    /// </summary>
    /// <remarks>
    /// An inset rule is what separates the rows of a list without cutting across the gutter the rows are
    /// indented by - the divider under an avatar row starts where the text does, which is "3rem 0". It
    /// shortens a horizontal separator at its start and its end, which follow the reading direction, and a
    /// vertical one at the top and the bottom. It is padding, so a percentage measures against the width of
    /// the container whichever way the separator runs, which is what CSS does with every percentage padding;
    /// a vertical separator wants an absolute length.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Inset { get; set; }

    /// <summary>
    /// The style the line of the separator is drawn in: solid, dashed, dotted or double.
    /// </summary>
    /// <remarks>
    /// A double line needs at least three pixels of <see cref="Thickness"/> to have room for its gap, and
    /// the dots of a hairline dotted line barely read without one either.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSeparatorLineStyle? LineStyle { get; set; }

    /// <summary>
    /// The size of the line of the separator, out of the sizes of the theme.
    /// </summary>
    /// <remarks>
    /// Small is the hairline every divider of the theme is drawn at, and each step up multiplies it, so a
    /// preset that thins its hairline thins the whole scale with it. <see cref="Thickness"/> sets a length
    /// of its own instead, and wins where both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the separator.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitSeparatorClassStyles? Styles { get; set; }

    /// <summary>
    /// The thickness of the line of the separator, as any CSS length.
    /// </summary>
    /// <remarks>
    /// Leaving it unset keeps the line at the weight of the current <see cref="Size"/>, which starts at the
    /// hairline the theme draws every divider at. A heavier rule used as a section break is what this is
    /// for - and the dots of a one-pixel dotted line barely read without it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Thickness { get; set; }

    /// <summary>
    /// Whether the element is a vertical separator.
    /// </summary>
    /// <remarks>
    /// A vertical separator stretches to the height of the flex row it stands in and takes it from its
    /// container anywhere else, but is never shorter than a line of text - so it stands between runs of text
    /// as readily as between the items of a toolbar.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Vertical { get; set; }



    protected override string RootElementClass => "bit-spr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => AlignContent switch
        {
            BitSeparatorAlignContent.Start => "bit-spr-srt",
            BitSeparatorAlignContent.End => "bit-spr-end",
            _ => "bit-spr-ctr"
        });

        // The background and the border kind classes carry a leading "b" so that the whole per-role vocabulary
        // of the theme (pri, pbg, pbr, ...) stays free for the Color parameter below, the same way BitCard
        // names the two apart.
        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-spr-bpg",
            BitColorKind.Secondary => "bit-spr-bsg",
            BitColorKind.Tertiary => "bit-spr-btg",
            BitColorKind.Transparent => "bit-spr-brg",
            _ => null
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-spr-bpr",
            BitColorKind.Secondary => "bit-spr-bsr",
            BitColorKind.Tertiary => "bit-spr-btr",
            BitColorKind.Transparent => "bit-spr-brr",
            _ => null
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-spr-pri",
            BitColor.Secondary => "bit-spr-sec",
            BitColor.Tertiary => "bit-spr-ter",
            BitColor.Info => "bit-spr-inf",
            BitColor.Success => "bit-spr-suc",
            BitColor.Warning => "bit-spr-wrn",
            BitColor.SevereWarning => "bit-spr-swr",
            BitColor.Error => "bit-spr-err",
            BitColor.PrimaryBackground => "bit-spr-pbg",
            BitColor.SecondaryBackground => "bit-spr-sbg",
            BitColor.TertiaryBackground => "bit-spr-tbg",
            BitColor.PrimaryForeground => "bit-spr-pfg",
            BitColor.SecondaryForeground => "bit-spr-sfg",
            BitColor.TertiaryForeground => "bit-spr-tfg",
            BitColor.PrimaryBorder => "bit-spr-pbr",
            BitColor.SecondaryBorder => "bit-spr-sbr",
            BitColor.TertiaryBorder => "bit-spr-tbr",
            _ => null
        });

        ClassBuilder.Register(() => LineStyle switch
        {
            BitSeparatorLineStyle.Solid => "bit-spr-sld",
            BitSeparatorLineStyle.Dashed => "bit-spr-dsh",
            BitSeparatorLineStyle.Dotted => "bit-spr-dot",
            BitSeparatorLineStyle.Double => "bit-spr-dbl",
            _ => null
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-spr-sm",
            BitSize.Medium => "bit-spr-md",
            BitSize.Large => "bit-spr-lg",
            _ => null
        });

        ClassBuilder.Register(() => Vertical ? "bit-spr-vrt" : "bit-spr-hrz");
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // A vertical separator stretches to the height of the flex row it stands in; sizing to the content
        // means opting out of that stretch as well as out of taking the height of a container that has one.
        StyleBuilder.Register(() => AutoSize is false ? string.Empty : Vertical ? "height:auto;align-self:auto" : "width:auto");

        StyleBuilder.Register(() => Thickness.HasNoValue() ? null : $"--bit-spr-siz:{Thickness}");

        StyleBuilder.Register(() => ContentOffset.HasNoValue() ? null : $"--bit-spr-ofs:{ContentOffset}");

        StyleBuilder.Register(() => Inset.HasNoValue() ? null : $"--bit-spr-ins:{Inset}");
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSeparatorParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var element = Element?.Trim();
        if (element.HasNoValue() || IsValidElement(element!) is false || IsVoidElement(element!))
        {
            element = DefaultElement;
        }

        builder.OpenElement(0, element!);
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "role", _role);
        builder.AddAttribute(4, "aria-label", Decorative ? null : AriaLabel);
        builder.AddAttribute(5, "aria-labelledby", GetAriaLabelledby());
        builder.AddAttribute(6, "aria-orientation", _ariaOrientation);
        builder.AddAttribute(7, "style", StyleBuilder.Value);
        builder.AddAttribute(8, "class", ClassBuilder.Value);
        builder.AddAttribute(9, "dir", Dir?.ToString().ToLower(CultureInfo.InvariantCulture));
        builder.AddElementReferenceCapture(10, v => RootElement = v);

        if (ChildContent is not null)
        {
            builder.OpenElement(11, "div");
            builder.AddAttribute(12, "id", _contentId);
            builder.AddAttribute(13, "style", Styles?.Content);
            builder.AddAttribute(14, "class", $"bit-spr-cnt {Classes?.Content}".TrimEnd());
            builder.AddContent(15, ChildContent);
            builder.CloseElement();
        }

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }



    // The children of a separator are presentational to assistive technologies, so the content names the
    // separator through aria-labelledby rather than being read out of it - unless an AriaLabel already does.
    // An aria-labelledby the page splats on names it after something elsewhere on the page, and is kept as
    // written, while a decorative separator is not a separator to name at all.
    private string? GetAriaLabelledby()
    {
        if (Decorative) return null;

        if (HtmlAttributes.TryGetValue("aria-labelledby", out var labelledby) && labelledby?.ToString() is { Length: > 0 } ids)
        {
            return ids;
        }

        return ChildContent is not null && AriaLabel.HasNoValue() ? _contentId : null;
    }
}
