// a fork from https://github.com/mariusmuntean/ChartJs.Blazor

using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Simple and flexible charting component for data visualization, which supports eight chart types: bar, line, area, pie, bubble, radar, polar, and scatter.
/// </summary>
public partial class BitChartLegacy : IAsyncDisposable
{
    private bool _disposed;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// The configuration of the chart.
    /// </summary>
    [Parameter] public BitChartLegacyConfigBase? Config { get; set; }

    /// <summary>
    /// The height of the canvas HTML element. 
    /// Use <see langword="null"/> when using <see cref="BitChartLegacyBaseConfigOptions.AspectRatio"/>.
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
    /// By default BitChartLegacy uses the date-fns adapter. you can change the adapter using <see cref="BitChartLegacy.DateAdapterScripts"/>.
    /// for more info check out https://www.chartjs.org/docs/2.9.4/axes/cartesian/time.html#date-adapters
    /// </summary>
    [Parameter] public bool IsDateAdapterRequired { get; set; }

    /// <summary>
    /// The list of scripts required for the customized chartjs date adapter.
    /// see available adapters here: https://github.com/chartjs/awesome#adapters
    /// </summary>
    [Parameter] public IEnumerable<string>? DateAdapterScripts { get; set; }



    /// <summary>
    /// Updates the chart.
    /// <para>
    /// Call this method after you've updated the <see cref="Config"/>.
    /// </para>
    /// </summary>
    public Task Update()
    {
        if (Config is null) return Task.CompletedTask;

        return _js.BitChartJsUpdateChart(Config).AsTask();
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBubbleConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLineConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyPieConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyPolarAreaConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyRadarConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyScatterConfig))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyConfigBase<,>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyConfigBase<>))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonStringEnumConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexableOptionConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FloatingBarPointConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ClippingJsonConverter))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonWriteOnlyConverter<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonObjectEnumConverter))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBubbleOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBaseConfigOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLineOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyPieOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyPolarAreaOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyRadarOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyIndexableOption<>))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarDataset<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBubbleDataset))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyDataset<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLineDataset<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyPieDataset))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyPolarAreaDataset))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyRadarDataset))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLegend))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyPosition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyTooltips))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyAnimation))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarScales))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyScales))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyCartesianTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyCategoryTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLinearCartesianTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLogarithmicTicks))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyTimeTicks))]

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarCategoryAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarLinearCartesianAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarLogarithmicAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyBarTimeAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyCartesianAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyCartesianAxis<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyCategoryAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLinearCartesianAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyLogarithmicAxis))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartLegacyTimeAxis))]
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _js.BitLegacyInitScripts(["_content/Bit.BlazorUI.Legacy/chart.js/chartjs-2.9.4.js"]);

            if (IsDateAdapterRequired && DateAdapterScripts is null)
            {
                await _js.BitLegacyInitScripts(["_content/Bit.BlazorUI.Legacy/chart.js/chartjs-2.9.4-adapter.js"]);
            }

            if (DateAdapterScripts is not null)
            {
                await _js.BitLegacyInitScripts(DateAdapterScripts);
            }

            if (Config is not null)
            {
                await _js.BitChartJsSetupChart(Config);
            }

            await SetupCompletedCallback.InvokeAsync(this);
            return;
        }

        if (Config is not null)
        {
            await _js.BitChartJsSetupChart(Config);
        }
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        try
        {
            if (Config is not null)
            {
                await _js.BitChartJsRemoveChart(Config.CanvasId);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
