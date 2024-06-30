namespace Bit.BlazorUI;

public partial class BitSeparator : BitComponentBase
{
    private bool isVertical;
    private BitSeparatorAlignContent separatorAlignContent = BitSeparatorAlignContent.Center;



    /// <summary>
    /// Where the content should be aligned in the separator.
    /// </summary>
    [Parameter]
    public BitSeparatorAlignContent AlignContent
    {
        get => separatorAlignContent;
        set
        {
            if (separatorAlignContent == value) return;

            separatorAlignContent = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The content of the Separator, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the element is a vertical separator.
    /// </summary>
    [Parameter]
    public bool IsVertical
    {
        get => isVertical;
        set
        {
            if (isVertical == value) return;

            isVertical = value;
            ClassBuilder.Reset();
        }
    }



    protected override string RootElementClass => "bit-spr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsVertical ? $"{RootElementClass}-vrt" : $"{RootElementClass}-hrz");

        ClassBuilder.Register(() => AlignContent switch
        {
            BitSeparatorAlignContent.Start => $"{RootElementClass}-srt",
            BitSeparatorAlignContent.End => $"{RootElementClass}-end",
            _ => $"{RootElementClass}-ctr"
        });
    }
}
