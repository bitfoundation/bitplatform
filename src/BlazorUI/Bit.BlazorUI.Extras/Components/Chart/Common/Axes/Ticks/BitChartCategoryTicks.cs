﻿namespace Bit.BlazorUI;

/// <summary>
/// The ticks-subconfig of <see cref="BitChartCategoryAxis"/>.
/// </summary>
public class BitChartCategoryTicks : BitChartCartesianTicks
{
    /// <summary>
    /// Gets or sets an array of labels to display.
    /// </summary>
    public List<string> Labels { get; set; }

    /// <summary>
    /// Gets or sets the minimum item to display. The item has to be present in <see cref="Labels"/>.
    /// <para>Read more <a href="https://www.chartjs.org/docs/latest/axes/cartesian/category.html#min-max-configuration"/>.</para>
    /// </summary>
    public new string Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum item to display. The item has to be present in <see cref="Labels"/>.
    /// <para>Read more <a href="https://www.chartjs.org/docs/latest/axes/cartesian/category.html#min-max-configuration"/>.</para>
    /// </summary>
    public new string Max { get; set; }
}
