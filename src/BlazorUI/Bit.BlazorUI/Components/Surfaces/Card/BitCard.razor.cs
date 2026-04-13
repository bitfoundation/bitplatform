using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A Card provides a container to wrap around a specific content. Keeping a card to a single subject keeps the design clean.
/// </summary>
public partial class BitCard : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the card component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple card components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitCardParams.ParamName)]
    public BitCardParams? CascadingParameters { get; set; }



    /// <summary>
    /// The color kind of the background of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// The content of the card.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Sets the shadow elevation level of the card (1-24). Maps to theme shadow variables (--bit-shd-1 to --bit-shd-24).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public int? Elevation { get; set; }

    /// <summary>
    /// Makes the card height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the card width and height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullSize { get; set; }

    /// <summary>
    /// Makes the card width 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Sets the height of the card explicitly.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Removes the default padding of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoPadding { get; set; }

    /// <summary>
    /// Removes the default shadow around the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoShadow { get; set; }

    /// <summary>
    /// Renders the card with no shadow and a primary border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Outlined { get; set; }

    /// <summary>
    /// Removes the border-radius from the card, rendering it with sharp corners.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Square { get; set; }

    /// <summary>
    /// Sets the width of the card explicitly.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    protected override string RootElementClass => "bit-crd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-crd-pbg",
            BitColorKind.Secondary => "bit-crd-sbg",
            BitColorKind.Tertiary => "bit-crd-tbg",
            BitColorKind.Transparent => "bit-crd-rbg",
            _ => "bit-crd-sbg"
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-crd-brd bit-crd-pbr",
            BitColorKind.Secondary => "bit-crd-brd bit-crd-sbr",
            BitColorKind.Tertiary => "bit-crd-brd bit-crd-tbr",
            BitColorKind.Transparent => "bit-crd-brd bit-crd-rbr",
            _ => ""
        });

        ClassBuilder.Register(() => FullSize || FullHeight ? "bit-crd-fhe" : string.Empty);
        ClassBuilder.Register(() => FullSize || FullWidth ? "bit-crd-fwi" : string.Empty);

        ClassBuilder.Register(() => Elevation is >= 1 and <= 24 ? $"bit-crd-e{Elevation}" : string.Empty);

        ClassBuilder.Register(() => NoPadding ? "bit-crd-npd" : string.Empty);

        ClassBuilder.Register(() => NoShadow ? "bit-crd-nsd" : string.Empty);

        ClassBuilder.Register(() => Outlined ? "bit-crd-otl" : string.Empty);

        ClassBuilder.Register(() => Square ? "bit-crd-sqr" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Height.HasNoValue() ? null : $"height:{Height}");

        StyleBuilder.Register(() => Width.HasNoValue() ? null : $"width:{Width}");
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCardParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);
        base.OnParametersSet();
    }
}
