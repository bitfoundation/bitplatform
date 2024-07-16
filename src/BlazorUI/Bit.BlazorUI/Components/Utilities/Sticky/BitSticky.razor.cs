﻿namespace Bit.BlazorUI;

public partial class BitSticky : BitComponentBase
{
    /// <summary>
    /// Specifying the vertical position of a positioned element from bottom.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Bottom { get; set; }

    /// <summary>
    /// The content of the Sticky, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from left.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Left { get; set; }

    /// <summary>
    /// Region to render sticky component in.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitStickyPosition? Position { get; set; }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from right.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Right { get; set; }

    /// <summary>
    /// Specifying the vertical position of a positioned element from top.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Top { get; set; }



    protected override string RootElementClass => "bit-stk";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Position switch
        {
            BitStickyPosition.Top => "bit-stk-top",
            BitStickyPosition.Bottom => "bit-stk-btm",
            BitStickyPosition.TopAndBottom => "bit-stk-tab",
            BitStickyPosition.Start => "bit-stk-srt",
            BitStickyPosition.End => "bit-stk-end",
            BitStickyPosition.StartAndEnd => "bit-stk-sae",
            _ => (Top is null && Bottom is null && Left is null && Right is null)
                    ? "bit-stk-top"
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
