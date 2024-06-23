namespace Bit.BlazorUI;

/// <summary>
/// Represents a dataset with an id and a type.
/// </summary>
public interface IBitChartDataset
{
    /// <summary>
    /// Gets the ID of this dataset. Used to keep track of the datasets
    /// across the .NET &lt;-&gt; JavaScript boundary.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the <see cref="BitChartChartType"/> this dataset is for.
    /// Important to set in mixed charts.
    /// </summary>
    BitChartChartType Type { get; }
}

/// <summary>
/// Represents a strongly typed dataset that holds data of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data this dataset contains.</typeparam>
public interface IDataset<T> : IBitChartDataset, IList<T>
{
    /// <summary>
    /// Gets the data contained in this dataset. This property is read-only.
    /// This is in addition to implementing <see cref="IList{T}"/>.
    /// </summary>
    IReadOnlyList<T> Data { get; }
}
