namespace Bit.BlazorUI;

/// <summary>
/// A helper class to ease the using of a list of the BitCascadingValue.
/// </summary>
public class BitCascadingValueList : List<BitCascadingValue>
{
    public BitCascadingValueList() { }

    public BitCascadingValueList(int capacity) : base(capacity) { }

    public BitCascadingValueList(IEnumerable<BitCascadingValue> collection) : base(collection) { }



    /// <summary>
    /// Adds a typed BitCascadingValue to the list. The ValueType of the created cascading value is the
    /// static type of <typeparamref name="T"/>, so null values, nullable value types, interfaces and
    /// base types are all cascaded as the type they are declared with.
    /// </summary>
    /// <param name="value">The value to be provided.</param>
    /// <param name="name">The optional name of the cascading value.</param>
    /// <param name="isFixed">Determines that the value will not change.</param>
    /// <param name="enabled">Determines that the value is provided at all.</param>
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
    public new void Add<T>(T value, string? name = null, bool isFixed = false, bool enabled = true)
        => base.Add(new BitCascadingValue(value, name, isFixed, typeof(T)) { Enabled = enabled });
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required

    /// <summary>
    /// Adds an already created BitCascadingValue to the list. A null item is ignored.
    /// </summary>
    public new void Add(BitCascadingValue? value)
    {
        if (value is null) return;

        base.Add(value);
    }

    /// <summary>
    /// Adds a BitCascadingValue with an explicit ValueType to the list, which is the way of cascading a
    /// value as one of its base types or interfaces when the type is only known at runtime.
    /// </summary>
    public void Add(object? value, Type valueType, string? name = null, bool isFixed = false, bool enabled = true)
        => base.Add(new BitCascadingValue(value, name, isFixed, valueType) { Enabled = enabled });

    /// <summary>
    /// Adds a typed BitCascadingValue to the list only when the given condition is true.
    /// </summary>
    public void AddIf<T>(bool condition, T value, string? name = null, bool isFixed = false)
    {
        if (condition is false) return;

        base.Add(new BitCascadingValue(value, name, isFixed, typeof(T)));
    }

    /// <summary>
    /// Adds a fixed (IsFixed) typed BitCascadingValue to the list. Fixed values never subscribe their
    /// consumers for change notifications, so they are the cheapest way of cascading a value that never changes.
    /// </summary>
    public void AddFixed<T>(T value, string? name = null) => base.Add(new BitCascadingValue(value, name, true, typeof(T)));
}
