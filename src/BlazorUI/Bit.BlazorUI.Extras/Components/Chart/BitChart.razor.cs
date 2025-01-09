// a fork from https://github.com/mariusmuntean/ChartJs.Blazor

using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Simple and flexible charting component for data visualization, which supports eight chart types: bar, line, area, pie, bubble, radar, polar, and scatter.
/// </summary>
public partial class BitChart : IAsyncDisposable
{
    [Inject] private IJSRuntime _js { get; set; }

    /// <summary>
    /// The configuration of the chart.
    /// </summary>
    [Parameter] public BitChartConfigBase Config { get; set; }

    /// <summary>
    /// The height of the canvas HTML element. 
    /// Use <see langword="null"/> when using <see cref="BitChartBaseConfigOptions.AspectRatio"/>.
    /// </summary>
    [Parameter] public int? Height { get; set; }

    /// <summary>
    /// This event is fired when the chart has been setup through interop and
    /// the JavaScript chart object is available. Use this callback if you need to setup
    /// custom JavaScript options or register plugins.
    /// </summary>
    [Parameter] public EventCallback SetupCompletedCallback { get; set; }

    /// <summary>
    /// The width of the canvas HTML element.
    /// </summary>
    [Parameter] public int? Width { get; set; }

    /// <summary>
    /// Whether the date adapter is required for the current configuration.
    /// By default BitChart uses the date-fns adapter. you can change the adapter using <see cref="BitChart.DateAdapterScripts"/>.
    /// for more info check out https://www.chartjs.org/docs/2.9.4/axes/cartesian/time.html#date-adapters
    /// </summary>
    [Parameter] public bool IsDateAdapterRequired { get; set; }

    /// <summary>
    /// The list of scripts required for the customized chartjs date adapter.
    /// see available adapters here: https://github.com/chartjs/awesome#adapters
    /// </summary>
    [Parameter] public IEnumerable<string>? DateAdapterScripts { get; set; }


    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBubbleConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLineConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartPieConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartPolarAreaConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartRadarConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartScatterConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartConfigBase<,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartConfigBase<>))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonStringEnumConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexableOptionConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FloatingBarPointConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ClippingJsonConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonWriteOnlyConverter<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonObjectEnumConverter))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBubbleOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBaseConfigOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLineOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartPieOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartPolarAreaOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartRadarOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartIndexableOption<>))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarDataset<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBubbleDataset))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartDataset<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLineDataset<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartPieDataset))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartPolarAreaDataset))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartRadarDataset))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegend))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartPosition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartTooltips))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartAnimation))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarScales))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartScales))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartCartesianTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartCategoryTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLinearCartesianTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLogarithmicTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartTimeTicks))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarCategoryAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarLinearCartesianAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarLogarithmicAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartBarTimeAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartCartesianAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartCartesianAxis<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartCategoryAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLinearCartesianAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLogarithmicAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartTimeAxis))]
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var scripts = new List<string> { "_content/Bit.BlazorUI.Extras/chart.js/chartjs-2.9.4.js" };

            if (IsDateAdapterRequired && DateAdapterScripts is null)
            {
                scripts.Add("_content/Bit.BlazorUI.Extras/chart.js/chartjs-2.9.4-adapter.js");
            }

            if (DateAdapterScripts is not null)
            {
                scripts.AddRange(DateAdapterScripts);
            }

            await _js.BitChartJsInitChartJs(scripts);

            await _js.BitChartJsSetupChart(Config);

            await SetupCompletedCallback.InvokeAsync(this);
        }
        else
        {
            await _js.BitChartJsSetupChart(Config);
        }
    }

    /// <summary>
    /// Updates the chart.
    /// <para>
    /// Call this method after you've updated the <see cref="Config"/>.
    /// </para>
    /// </summary>
    public Task Update()
    {
        return _js.BitChartJsUpdateChart(Config).AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing is false) return;

        try
        {
            await _js.BitChartJsRemoveChart(Config?.CanvasId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
