namespace Bit.BlazorUI;

/// <summary>
/// A Separator is a component that visually separates content into groups.
/// </summary>
/// <remarks>
/// The line runs horizontally across its container unless <see cref="Vertical"/> stands it up, and any
/// <see cref="ChildContent"/> - a label, an icon - sits on the line where <see cref="AlignContent"/> puts
/// it, nudged from the edge by <see cref="ContentOffset"/>.
/// The line itself is drawn by the theme and restyled through <see cref="LineStyle"/>, <see cref="Thickness"/>
/// and <see cref="Color"/>, while <see cref="Background"/> and <see cref="Border"/> keep it on the neutral
/// surface tiers.
/// <br />
/// To assistive technologies the root reports itself as a separator, named by its content or by an
/// <see cref="BitComponentBase.AriaLabel"/>; a separator that is only visual sugar opts out of being
/// announced at all through <see cref="Decorative"/>.
/// </remarks>
public partial class BitSeparator : BitComponentBase
{
    private string _contentId => $"{_Id}-cnt";

    private string? _role => Decorative ? "none" : "separator";

    // aria-orientation implicitly defaults to horizontal on the separator role, so only vertical needs saying.
    private string? _ariaOrientation => Decorative is false && Vertical ? "vertical" : null;

    // The children of a separator are presentational to assistive technologies, so the content names the
    // separator through aria-labelledby rather than being read out of it - unless an AriaLabel already does.
    private string? _ariaLabelledby => ChildContent is not null && Decorative is false && AriaLabel.HasNoValue() ? _contentId : null;



    /// <summary>
    /// Where the content should be aligned in the separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSeparatorAlignContent? AlignContent { get; set; }

    /// <summary>
    /// Renders the separator with auto width or height.
    /// </summary>
    /// <remarks>
    /// A horizontal separator is as wide as its container and a vertical one as tall; this lets it size to
    /// its content instead, which is what a separator stretched by a flex container's align-items wants.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public bool AutoSize { get; set; }

    /// <summary>
    /// The color kind of the background of the content of the separator.
    /// </summary>
    /// <remarks>
    /// The content sits on the line and masks it with this background, so it matches the surface the
    /// separator is drawn on - a separator on a secondary-colored panel wants a secondary background, and
    /// one over a picture wants a transparent one.
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
    /// It sits on the line where <see cref="AlignContent"/> puts it, and it also names the separator to
    /// assistive technologies - the children of a separator are presentational, so the name is wired through
    /// aria-labelledby rather than read out of the line.
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
    /// edge to be offset from. It is direction-aware on a horizontal separator, and on a vertical one it
    /// pushes the content down from the top or up from the bottom.
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
    /// The style the line of the separator is drawn in: solid, dashed or dotted.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSeparatorLineStyle? LineStyle { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the separator.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitSeparatorClassStyles? Styles { get; set; }

    /// <summary>
    /// The thickness of the line of the separator, as any CSS length.
    /// </summary>
    /// <remarks>
    /// Leaving it unset keeps the hairline the theme draws every divider at. A heavier rule used as a
    /// section break is what this is for - and the dots of a one-pixel dotted line barely read without it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Thickness { get; set; }

    /// <summary>
    /// Whether the element is a vertical separator.
    /// </summary>
    /// <remarks>
    /// A vertical separator takes its height from its container, so give the container a height - or stand
    /// it in a flex row, where <see cref="AutoSize"/> lets the row's alignment stretch it.
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
            BitSeparatorLineStyle.Dashed => "bit-spr-dsh",
            BitSeparatorLineStyle.Dotted => "bit-spr-dot",
            _ => null
        });

        ClassBuilder.Register(() => Vertical ? "bit-spr-vrt" : "bit-spr-hrz");
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => AutoSize ? (Vertical ? "height:auto" : "width:auto") : string.Empty);

        StyleBuilder.Register(() => Thickness.HasNoValue() ? null : $"--bit-spr-siz:{Thickness}");

        StyleBuilder.Register(() => ContentOffset.HasNoValue() ? null : $"--bit-spr-ofs:{ContentOffset}");
    }
}
