namespace Bit.BlazorUI;

/// <summary>
/// A Separator is a component that visually separates content into groups.
/// </summary>
public partial class BitSeparator : BitComponentBase
{
    /// <summary>
    /// Where the content should be aligned in the separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSeparatorAlignContent? AlignContent { get; set; }

    /// <summary>
    /// Renders the separator with auto width or height.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool AutoSize { get; set; }

    /// <summary>
    /// The color kind of the background of the separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// The content of the Separator, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the element is a vertical separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    protected override string RootElementClass => "bit-spr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => AlignContent switch
        {
            BitSeparatorAlignContent.Start => "bit-spr-srt",
            BitSeparatorAlignContent.End => "bit-spr-end",
            _ => "bit-spr-ctr"
        });

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-spr-pbg",
            BitColorKind.Secondary => "bit-spr-sbg",
            BitColorKind.Tertiary => "bit-spr-tbg",
            BitColorKind.Transparent => "bit-spr-rbg",
            _ => null
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-spr-pbr",
            BitColorKind.Secondary => "bit-spr-sbr",
            BitColorKind.Tertiary => "bit-spr-tbr",
            BitColorKind.Transparent => "bit-spr-rbr",
            _ => null
        });

        ClassBuilder.Register(() => Vertical ? "bit-spr-vrt" : "bit-spr-hrz");
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => AutoSize ? (Vertical ? "height:auto" : "width:auto") : string.Empty);
    }
}
