namespace Bit.BlazeDb;

/// <summary>
/// An optional inclusive range bound. Implicitly convertible from a value, so call sites read
/// naturally: <c>table.Range(index, from: date1, to: BlazeDbBound&lt;DateTime&gt;.Unbounded)</c>.
/// </summary>
public readonly struct BlazeDbBound<T>
{
    private BlazeDbBound(T value)
    {
        Value = value;
        HasValue = true;
    }

    public bool HasValue { get; }

    public T Value { get; }

    public static BlazeDbBound<T> Unbounded => default;

    public static BlazeDbBound<T> At(T value) => new(value);

    public static implicit operator BlazeDbBound<T>(T value) => new(value);
}
