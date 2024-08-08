﻿namespace Bit.BlazorUI;

public partial class BitSpacer : BitComponentBase
{
    /// <summary>
    /// Gets or sets the width of the spacer (in pixels).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? Width { get; set; }



    protected override string RootElementClass => "bit-spc";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Width.HasValue ? $"margin-inline-start:{Width}px" : "flex-grow:1");
    }
}
