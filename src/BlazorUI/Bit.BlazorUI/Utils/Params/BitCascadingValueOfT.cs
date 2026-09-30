using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A <see cref="BitCascadingValue"/> whose ValueType is <typeparamref name="T"/>, so its value is read and
/// assigned as a <typeparamref name="T"/> - the typed counterpart of the framework's CascadingValueSource.
/// It goes anywhere a BitCascadingValue does, and it is what every value created from a static type is:
/// the From, Fixed, Lazy, Computed and Observed factories, the typed helpers of
/// <see cref="BitCascadingValueList"/> and the implicit conversions all create one, while a value created
/// with an explicit <see cref="Type"/> or through the untyped constructors is a plain BitCascadingValue.
/// </summary>
public class BitCascadingValue<T> : BitCascadingValue
{
    /// <summary>
    /// Creates a cascading value that is cascaded as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">The value to be provided.</param>
    /// <param name="name">The optional name of the cascading value.</param>
    /// <param name="isFixed">Determines that the value will not change.</param>
    /// <param name="enabled">Determines that the value is provided at all.</param>
    public BitCascadingValue(T value, string? name = null, bool isFixed = false, bool enabled = true)
        : base(value, name, isFixed, typeof(T), enabled) { }

    internal BitCascadingValue(Func<object?> valueFactory, bool isComputed, string? name, bool isFixed, bool enabled)
        : base(valueFactory, isComputed, typeof(T), name, isFixed, enabled) { }



    /// <summary>
    /// The value to be provided, typed as <typeparamref name="T"/>. Assigning a different value refreshes the
    /// consumers; a lazy factory runs on the first read, a computed one on every read. It may be null even
    /// when <typeparamref name="T"/> is a non-nullable reference type, since the untyped
    /// <see cref="BitCascadingValue.Value"/> it shares its storage with accepts null for any reference type.
    /// </summary>
    [MaybeNull]
    public new T Value
    {
        get => (T)base.Value!;
        set => base.Value = value;
    }



    /// <summary>
    /// Assigns <paramref name="newValue"/> to <see cref="Value"/> and returns a task that completes once every
    /// listening <see cref="BitCascadingValueProvider"/> has re-rendered with it, which is the counterpart of
    /// the NotifyChangedAsync(newValue) method of the framework's CascadingValueSource. Unlike assigning
    /// <see cref="Value"/>, the consumers are refreshed even when the new value equals the current one.
    /// Like assigning <see cref="Value"/>, it replaces a lazy or a computed factory for good, so a computed
    /// value stops tracking its source from then on.
    /// </summary>
    public Task NotifyChangedAsync(T newValue)
    {
        StoreValue(newValue);

        return NotifyChangedAsync();
    }
}
