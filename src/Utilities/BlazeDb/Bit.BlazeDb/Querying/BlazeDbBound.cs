namespace Bit.BlazeDb;

/// <summary>
/// An optional range bound, inclusive unless made with <see cref="Exclusive"/>. Implicitly
/// convertible from a value, so call sites read naturally:
/// <c>table.Range(index, from: date1, to: BlazeDbBound&lt;DateTime&gt;.Unbounded)</c>.
/// </summary>
public readonly struct BlazeDbBound<T>
{
    private BlazeDbBound(T value, bool exclusive)
    {
        Value = value;
        HasValue = true;
        IsExclusive = exclusive;
    }

    public bool HasValue { get; }

    public T Value { get; }

    /// <summary>
    /// True when rows whose indexed value equals <see cref="Value"/> are outside the range. Keyset
    /// paging uses it: the next page starts strictly after the last value the previous one showed.
    /// </summary>
    public bool IsExclusive { get; }

    public static BlazeDbBound<T> Unbounded => default;

    /// <summary>A bound that includes <paramref name="value"/> itself.</summary>
    public static BlazeDbBound<T> At(T value) => new(value, exclusive: false);

    /// <summary>
    /// A bound that stops short of <paramref name="value"/>: as a lower bound the range starts after
    /// it, as an upper bound it ends before it.
    /// </summary>
    public static BlazeDbBound<T> Exclusive(T value) => new(value, exclusive: true);

    public static implicit operator BlazeDbBound<T>(T value) => new(value, exclusive: false);
}
