
namespace Bit.BlazorUI;

/// <summary>
/// A drawing plugin, mirroring the Chart.js plugin concept. Plugins receive a
/// <see cref="BitChartPluginContext"/> with the computed scene, plotting area and scale conversions,
/// and can inject extra SVG primitives behind or in front of the datasets.
/// </summary>
public interface IBitChartPlugin
{
    /// <summary>Unique plugin id.</summary>
    string Id { get; }

    /// <summary>Called after scales/grid are computed but before datasets are drawn.</summary>
    void BeforeDatasetsDraw(BitChartPluginContext ctx) { }

    /// <summary>Called after datasets are drawn.</summary>
    void AfterDatasetsDraw(BitChartPluginContext ctx) { }

    /// <summary>
    /// What a screen reader is told about what the plugin draws, one sentence per mark. Everything drawn in the plot
    /// is hidden from assistive technologies, so a target line or a total in a doughnut's cutout that says nothing
    /// here is information only a sighted reader gets. The chart lists the sentences in a visually hidden note its
    /// plot is described by. The default says nothing, which is right for a purely decorative plugin. It is handed
    /// the same context the drawing was, so a plugin says only what it actually drew - nothing on a chart type it does
    /// not draw on, nothing for a dataset that is hidden.
    /// </summary>
    IEnumerable<string> Describe(BitChartPluginContext ctx) => [];
}
