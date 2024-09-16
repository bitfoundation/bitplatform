﻿namespace Bit.BlazorUI;

/// <summary>
/// Base class for chart-configs
/// <para>Contains the most basic required information about a chart.</para>
/// </summary>
public abstract class BitChartConfigBase
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartConfigBase"/>.
    /// </summary>
    /// <param name="chartType">The <see cref="BitChartChartType"/> this config is for.</param>
    protected BitChartConfigBase(BitChartChartType chartType)
    {
        Type = chartType;
    }

    /// <summary>
    /// Gets the type of chart this config is for.
    /// </summary>
    public BitChartChartType Type { get; }

    /// <summary>
    /// Gets the id for the html canvas element associated with this chart.
    /// This property is initialized to a random GUID-string upon creation.
    /// </summary>
    public string CanvasId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the list of inline plugins for this chart. Consider
    /// registering global plugins (through JavaScript) or assigning the
    /// plugins through JavaScript instead of using this property
    /// since these plugins work mostly with method hooks and methods
    /// can't be serialized. It could be supported, but just isn't yet.
    /// <para>
    /// Reference for Chart.js inline plugins:
    /// <a href="https://www.chartjs.org/docs/latest/developers/plugins.html#using-plugins"/>.
    /// </para>
    /// <para>
    /// For configuring plugins (plugin options), you need to use
    /// <see cref="BitChartBaseConfigOptions.Plugins"/> instead.
    /// </para>
    /// </summary>
    public List<object> Plugins { get; } = new List<object>();

    /// <summary>
    /// This method tells json.net to only serialize the plugins when there
    /// are plugins, don't call it directly.
    /// </summary>
    [Obsolete("Only for json.net, don't call it.", true)]
    public bool ShouldSerializePlugins() => Plugins.Count > 0;
}

/// <summary>
/// Base class for chart-configs which contains the options and the data subconfigs.
/// </summary>
/// <typeparam name="TOptions">The type of the options subconfig.</typeparam>
/// <typeparam name="TData">The type of the data subconfig.</typeparam>
public abstract class BitChartConfigBase<TOptions, TData> : BitChartConfigBase
    where TOptions : BitChartBaseConfigOptions
    where TData : BitChartChartData, new()
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartConfigBase"/>.
    /// </summary>
    /// <param name="chartType">The <see cref="BitChartChartType"/> this config is for.</param>
    protected BitChartConfigBase(BitChartChartType chartType) : base(chartType)
    {
        Data = new TData();
    }

    /// <summary>
    /// Gets or sets the options subconfig for this chart.
    /// </summary>
    public TOptions Options { get; set; }

    /// <summary>
    /// Gets the data subconfig for this chart.
    /// </summary>
    public TData Data { get; }
}

/// <inheritdoc cref="BitChartConfigBase{TOptions, TData}"/>
public abstract class BitChartConfigBase<TOptions> : BitChartConfigBase<TOptions, BitChartChartData>
    where TOptions : BitChartBaseConfigOptions
{
    /// <inheritdoc/>
    protected BitChartConfigBase(BitChartChartType chartType) : base(chartType) { }
}
