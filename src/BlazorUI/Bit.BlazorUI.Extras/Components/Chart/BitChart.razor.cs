// a fork from https://github.com/mariusmuntean/ChartJs.Blazor

namespace Bit.BlazorUI;

/// <summary>
/// Represents a Chart.js chart.
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

            await _js.InitChartJs(scripts);

            await _js.SetupChart(Config);

            await SetupCompletedCallback.InvokeAsync(this);
        }
        else
        {
            await _js.SetupChart(Config);
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
        return _js.UpdateChart(Config).AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing is false) return;

        await _js.RemoveChart(Config?.CanvasId);
    }
}
