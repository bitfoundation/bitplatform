namespace Bit.BlazorUI;

/// <summary>
/// A Card provides a container to wrap around a specific content. Keeping a card to a single subject keeps the design clean.
/// </summary>
public partial class BitCard : BitComponentBase
{
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
    }
}
