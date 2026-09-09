
namespace Bit.BlazorUI;

/// <summary>Context passed to plugins, exposing the scene, area and scale conversions.</summary>
public sealed class BitChartPluginContext
{
    public required BitChartScene Scene { get; init; }
    public required BitChartConfig Config { get; init; }
    public BitChartArea? Plot { get; init; }
    public bool IsCartesian { get; init; }

    // ---- Circular / radial geometry (pie/doughnut/polar/radar) ----
    /// <summary>Center of a circular/radial chart, if applicable.</summary>
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    /// <summary>Inner radius (doughnut cutout) in pixels.</summary>
    public double InnerRadius { get; init; }
    /// <summary>Outer radius in pixels.</summary>
    public double OuterRadius { get; init; }

    internal BitChartAxisScale? IndexScale { get; init; }
    internal Dictionary<string, BitChartAxisScale>? ValueScales { get; init; }

    /// <summary>Datasets the legend has toggled off at runtime.</summary>
    internal IReadOnlySet<int>? HiddenDatasets { get; init; }

    /// <summary>
    /// Whether the dataset at the given index is actually drawn. A dataset is hidden either by its own
    /// <see cref="BitChartDataset.Hidden"/> flag or by having been toggled off through the legend, and
    /// anything drawn from a dataset's values has to honour both or it outlives the series it describes.
    /// </summary>
    public bool IsDatasetVisible(int datasetIndex)
    {
        var datasets = Config.Data.Datasets;
        if (datasetIndex < 0 || datasetIndex >= datasets.Count) return false;
        return !datasets[datasetIndex].Hidden && HiddenDatasets?.Contains(datasetIndex) != true;
    }

    /// <summary>True when the index axis is a category axis, so indexes - not raw values - place things along it.</summary>
    public bool IndexIsCategory { get; init; }

    /// <summary>
    /// Whether categories sit in the middle of their band rather than on its edge. Bars force the
    /// centered layout, so anything drawn over the data has to follow the same choice or it lands half
    /// a band away from the points it is annotating.
    /// </summary>
    public bool IndexCentered { get; init; }

    /// <summary>Pixel position along the index axis for a category index. Follows the chart's own
    /// band placement unless the caller overrides it.</summary>
    public double XForIndex(int index, bool? centered = null)
        => IndexScale?.PixelForIndex(index, centered ?? IndexCentered) ?? 0;

    /// <summary>Pixel position along the index axis for a raw value (linear/time axes).</summary>
    public double XForValue(double value) => IndexScale?.PixelFor(value) ?? 0;

    /// <summary>Pixel position along a value axis (default "y").</summary>
    public double YForValue(double value, string axisId = "y")
    {
        if (ValueScales is null) return 0;
        if (ValueScales.TryGetValue(axisId, out var s)) return s.PixelFor(value);
        return ValueScales.Values.FirstOrDefault()?.PixelFor(value) ?? 0;
    }

    public void AddBehind(BitChartSvgNode node) => Scene.Background.Add(node);
    public void AddFront(BitChartSvgNode node) => Scene.Foreground.Add(node);
}
