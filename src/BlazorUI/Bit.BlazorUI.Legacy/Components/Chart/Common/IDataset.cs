namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a strongly typed dataset that holds data of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data this dataset contains.</typeparam>
public interface IDataset<T> : IBitChartLegacyDataset, IList<T>
{
    /// <summary>
    /// Gets the data contained in this dataset. This property is read-only.
    /// This is in addition to implementing <see cref="IList{T}"/>.
    /// </summary>
    IReadOnlyList<T> Data { get; }
}
