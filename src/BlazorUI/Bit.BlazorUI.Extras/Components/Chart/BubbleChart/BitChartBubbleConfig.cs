﻿namespace Bit.BlazorUI;

/// <summary>
/// Represents the config for a bubble chart.
/// </summary>
public class BitChartBubbleConfig : BitChartConfigBase<BitChartLineOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartBubbleConfig"/>.
    /// </summary>
    public BitChartBubbleConfig() : base(BitChartChartType.Bubble) { }
}
