namespace Bit.BlazorUI;

public partial class BitSticky : BitComponentBase
{
    private BitStickyPosition? position;



    /// <summary>
    /// Specifying the vertical position of a positioned element from bottom.
    /// </summary>
    [Parameter] public string? Bottom { get; set; }

    /// <summary>
    /// The content of the Sticky, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from left.
    /// </summary>
    [Parameter] public string? Left { get; set; }

    /// <summary>
    /// Region to render sticky component in.
    /// </summary>
    [Parameter]
    public BitStickyPosition? Position
    {
        get => position;
        set
        {
            if (position == value) return;

            position = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from right.
    /// </summary>
    [Parameter] public string? Right { get; set; }

    /// <summary>
    /// Specifying the vertical position of a positioned element from top.
    /// </summary>
    [Parameter] public string? Top { get; set; }


    protected override string RootElementClass => "bit-stk";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Position switch
        {
            BitStickyPosition.Top => $"{RootElementClass}-top",
            BitStickyPosition.Bottom => $"{RootElementClass}-btm",
            BitStickyPosition.TopAndBottom => $"{RootElementClass}-tab",
            BitStickyPosition.Start => $"{RootElementClass}-srt",
            BitStickyPosition.End => $"{RootElementClass}-end",
            BitStickyPosition.StartAndEnd => $"{RootElementClass}-sae",
            _ => (Top is null && Bottom is null && Left is null && Right is null)
                    ? $"{RootElementClass}-top"
                    : string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Top.HasValue() ? $"top: {Top}" : string.Empty);
        StyleBuilder.Register(() => Bottom.HasValue() ? $"bottom: {Bottom}" : string.Empty);
        StyleBuilder.Register(() => Left.HasValue() ? $"left: {Left}" : string.Empty);
        StyleBuilder.Register(() => Right.HasValue() ? $"right: {Right}" : string.Empty);
    }
}
