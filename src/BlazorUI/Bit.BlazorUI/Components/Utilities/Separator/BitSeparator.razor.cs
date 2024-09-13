namespace Bit.BlazorUI;

public partial class BitSeparator : BitComponentBase
{
    /// <summary>
    /// Where the content should be aligned in the separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSeparatorAlignContent AlignContent { get; set; } = BitSeparatorAlignContent.Center;

    /// <summary>
    /// The content of the Separator, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Renders the separator in full width of its container.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Whether the element is a vertical separator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    protected override string RootElementClass => "bit-spr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Vertical ? "bit-spr-vrt" : "bit-spr-hrz");

        ClassBuilder.Register(() => AlignContent switch
        {
            BitSeparatorAlignContent.Start => "bit-spr-srt",
            BitSeparatorAlignContent.End => "bit-spr-end",
            _ => "bit-spr-ctr"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => FullWidth ? "width:100%" : string.Empty);
    }
}
