using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Bit.BlazorUI;

/// <summary>
/// Represents a dataset containing a collection of values.
/// </summary>
/// <typeparam name="T">The type of data this <see cref="BitChartDataset{T}"/> contains.</typeparam>
[JsonObject]
public abstract class BitChartDataset<T> : Collection<T>, IDataset<T>
{
    /// <summary>
    /// Gets the ID of this dataset. Used to keep track of the datasets
    /// across the .NET &lt;-&gt; JavaScript boundary.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the data contained in this dataset. This property is read-only.
    /// This is in addition to implementing <see cref="IList{T}"/>.
    /// </summary>
    public IReadOnlyList<T> Data { get; }

    /// <summary>
    /// Gets the <see cref="BitChartChartType"/> this dataset is for.
    /// Important to set in mixed charts.
    /// </summary>
    public BitChartChartType Type { get; }

    /// <summary>
    /// Creates a new <see cref="BitChartDataset{T}"/>.
    /// </summary>
    /// <param name="type">The <see cref="BitChartChartType"/> this dataset is for.</param>
    /// <param name="id">The id for this dataset. If <see langword="null"/>,
    /// a random GUID string will be used.</param>
    protected BitChartDataset(BitChartChartType type, string id = null) : base(new List<T>())
    {
        Data = new ReadOnlyCollection<T>(Items);
        Id = id ?? Guid.NewGuid().ToString();
        Type = type;
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="BitChartDataset{T}"/>.
    /// </summary>
    /// <param name="items">
    /// The collection whose elements should be added to the end of the <see cref="BitChartDataset{T}"/>.
    /// </param>
    public void AddRange(IEnumerable<T> items) => ((List<T>)Items).AddRange(items ?? throw new ArgumentNullException(nameof(items)));

    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="BitChartDataset{T}"/>.
    /// </summary>
    /// <param name="items">
    /// The collection whose elements should be added to the end of the <see cref="BitChartDataset{T}"/>.
    /// </param>
    public void AddRange(params T[] items) => AddRange(items as IEnumerable<T>);

    /// <summary>
    /// Determines whether the specified object is considered equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><see langword="true"/> if the specified object is considered to be equal
    /// to the current object; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object obj) => obj is BitChartDataset<T> set &&
            Id == set.Id &&
            EqualityComparer<IList<T>>.Default.Equals(Items, set.Items);

    /// <summary>
    /// Returns the hash code for this <see cref="BitChartDataset{T}"/>.
    /// </summary>
    /// <returns>The hash code for this <see cref="BitChartDataset{T}"/>.</returns>
    public override int GetHashCode() => HashCode.Combine(Items, Id);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static bool operator ==(BitChartDataset<T> left, BitChartDataset<T> right) =>
            EqualityComparer<BitChartDataset<T>>.Default.Equals(left, right);

    public static bool operator !=(BitChartDataset<T> left, BitChartDataset<T> right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
