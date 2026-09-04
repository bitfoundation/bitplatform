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
        => base.Add(new BitCascadingValue(value, name, isFixed, typeof(T), enabled));
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
        => base.Add(new BitCascadingValue(value, name, isFixed, valueType, enabled));

    /// <summary>
    /// Adds a typed BitCascadingValue to the list only when the given condition is true.
    /// </summary>
    public void AddIf<T>(bool condition, T value, string? name = null, bool isFixed = false, bool enabled = true)
    {
        if (condition is false) return;

        base.Add(new BitCascadingValue(value, name, isFixed, typeof(T), enabled));
    }

    /// <summary>
    /// Adds an already created BitCascadingValue to the list only when the given condition is true.
    /// A null item is ignored. Pairing it with a lazily created BitCascadingValue is how the value of a
    /// conditional entry is kept from being built at all when the condition does not hold.
    /// </summary>
    public void AddIf(bool condition, BitCascadingValue? value)
    {
        if (condition is false || value is null) return;

        base.Add(value);
    }

    /// <summary>
    /// Adds a fixed (IsFixed) typed BitCascadingValue to the list. Fixed values never subscribe their
    /// consumers for change notifications, so they are the cheapest way of cascading a value that never changes.
    /// </summary>
    public void AddFixed<T>(T value, string? name = null) => base.Add(new BitCascadingValue(value, name, true, typeof(T)));

    /// <summary>
    /// Adds a fixed (IsFixed) BitCascadingValue with an explicit ValueType to the list, for when the
    /// cascaded type of a value that never changes is only known at runtime.
    /// </summary>
    public void AddFixed(object? value, Type valueType, string? name = null) => base.Add(new BitCascadingValue(value, name, true, valueType));

    /// <summary>
    /// Adds a typed BitCascadingValue whose value is produced by <paramref name="valueFactory"/> the first
    /// time it is actually needed, so an expensive value is never built for a disabled entry, for an entry
    /// that a later one shadows, or for a provider that is never rendered. The factory runs at most once.
    /// </summary>
    public void AddLazy<T>(Func<T> valueFactory, string? name = null, bool isFixed = false, bool enabled = true)
        => base.Add(BitCascadingValue.Lazy(valueFactory, name, isFixed, enabled));

    /// <summary>
    /// Adds a lazily produced BitCascadingValue with an explicit ValueType to the list, for when the
    /// cascaded type of a deferred value is only known at runtime. The factory runs at most once.
    /// </summary>
    public void AddLazy(Func<object?> valueFactory, Type valueType, string? name = null, bool isFixed = false, bool enabled = true)
        => base.Add(BitCascadingValue.Lazy(valueFactory, valueType, name, isFixed, enabled));

    /// <summary>
    /// Adds a typed BitCascadingValue that is re-read from <paramref name="valueFactory"/> on every render,
    /// so one list built once keeps tracking the state its values are derived from.
    /// </summary>
    public void AddComputed<T>(Func<T> valueFactory, string? name = null, bool isFixed = false)
        => base.Add(BitCascadingValue.Computed(valueFactory, name, isFixed));

    /// <summary>
    /// Adds a computed BitCascadingValue with an explicit ValueType to the list, for when the cascaded type
    /// of a value that is re-read on every render is only known at runtime.
    /// </summary>
    public void AddComputed(Func<object?> valueFactory, Type valueType, string? name = null, bool isFixed = false)
        => base.Add(BitCascadingValue.Computed(valueFactory, valueType, name, isFixed));

    /// <summary>
    /// Adds a typed BitCascadingValue that watches the value itself, so an object reporting its own changes
    /// through INotifyPropertyChanged or INotifyCollectionChanged refreshes the consumers on its own.
    /// </summary>
    public void AddObserved<T>(T value, string? name = null, bool enabled = true)
        => base.Add(BitCascadingValue.Observed(value, name, enabled));



    /// <summary>
    /// Finds the cascading value that the given type and name resolve to, which is the LAST entry matching
    /// both, since that is the one shadowing all the others. The name is matched case-insensitively,
    /// exactly like the consumers match it. Returns null when there is no such entry.
    /// </summary>
    public BitCascadingValue? Find(Type valueType, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        for (int i = Count - 1; i >= 0; i--)
        {
            var item = this[i];

            if (item is null) continue;
            if (item.ValueType != valueType) continue;
            if (string.Equals(item.Name, NormalizeName(name), StringComparison.OrdinalIgnoreCase) is false) continue;

            return item;
        }

        return null;
    }

    /// <summary>
    /// Finds the cascading value that the static type of <typeparamref name="T"/> and the given name
    /// resolve to, which is the last entry matching both. Returns null when there is no such entry.
    /// </summary>
    public BitCascadingValue? Find<T>(string? name = null) => Find(typeof(T), name);

    /// <summary>
    /// Whether the list holds a cascading value of the static type of <typeparamref name="T"/> carrying the
    /// given name, regardless of whether that entry is enabled.
    /// </summary>
    public bool Contains<T>(string? name = null) => Find(typeof(T), name) is not null;

    /// <summary>
    /// Removes every cascading value of the given type and name from the list, and reports whether anything
    /// was removed. The name is matched case-insensitively, exactly like the consumers match it.
    /// </summary>
    public bool Remove(Type valueType, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        var normalized = NormalizeName(name);

        return RemoveAll(item => item is not null
                              && item.ValueType == valueType
                              && string.Equals(item.Name, normalized, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    /// <summary>
    /// Removes every cascading value of the static type of <typeparamref name="T"/> carrying the given name
    /// from the list, and reports whether anything was removed.
    /// </summary>
    public bool Remove<T>(string? name = null) => Remove(typeof(T), name);

    /// <summary>
    /// Replaces every cascading value of the static type of <typeparamref name="T"/> carrying the given name
    /// with a new one holding <paramref name="value"/>, or appends it when the list has none, so a list that
    /// is kept around ends up with exactly one entry per type and name. The replacement takes the place of
    /// the first entry it replaces, which is what keeps the precedence of the list unchanged.
    /// </summary>
    public void Set<T>(T value, string? name = null, bool isFixed = false, bool enabled = true)
    {
        var created = new BitCascadingValue(value, name, isFixed, typeof(T), enabled);
        var normalized = NormalizeName(name);
        var index = FindIndex(item => item is not null
                                   && item.ValueType == typeof(T)
                                   && string.Equals(item.Name, normalized, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            base.Add(created);
            return;
        }

        Remove(typeof(T), name);

        Insert(index, created);
    }



    private static string? NormalizeName(string? name) => string.IsNullOrWhiteSpace(name) ? null : name;
}
