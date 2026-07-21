namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a dataset with an id and a type.
/// </summary>
public interface IBitChartLegacyDataset
{
    /// <summary>
    /// Gets the ID of this dataset. Used to keep track of the datasets
    /// across the .NET &lt;-&gt; JavaScript boundary.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the <see cref="BitChartLegacyChartType"/> this dataset is for.
    /// Important to set in mixed charts.
    /// </summary>
    BitChartLegacyChartType Type { get; }
}
