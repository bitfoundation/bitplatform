namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents an object that can be either a single value or an array of values. This is used for type safe js-interop.
/// </summary>
/// <typeparam name="T">The type of data this <see cref="BitChartLegacyIndexableOption{T}"/> is supposed to hold.</typeparam>
[Newtonsoft.Json.JsonConverter(typeof(IndexableOptionConverter))]   // newtonsoft for now
public class BitChartLegacyIndexableOption<T> : IEquatable<BitChartLegacyIndexableOption<T>>
{
    /// <summary>
    /// The compile-time name of the property which gets the wrapped value. This is used internally for serialization.
    /// </summary>
    internal const string PropertyName = nameof(BoxedValue);

    // for serialization, there has to be a cast to object anyway
    internal object? BoxedValue => IsIndexed ? IndexedValues : SingleValue;

    private readonly T? _singleValue;
    private readonly T[]? _indexedValues;

    /// <summary>
    /// The indexed values represented by this instance.
    /// </summary>
    public T[]? IndexedValues
    {
        get
        {
            if (!IsIndexed)
                throw new InvalidOperationException("This instance represents a single value. The indexed values are not available.");

            return _indexedValues;
        }
    }

    /// <summary>
    /// The single value represented by this instance.
    /// </summary>
    public T? SingleValue
    {
        get
        {
            if (IsIndexed)
                throw new InvalidOperationException("This instance represents an array of values. The single value is not available.");

            return _singleValue;
        }
    }

    /// <summary>
    /// Gets the value indicating whether the option wrapped in this <see cref="BitChartLegacyIndexableOption{T}"/> is indexed.
    /// <para>True if the wrapped value represents an array of <typeparamref name="T"/>, false if it represents a single value of <typeparamref name="T"/>.</para>
    /// </summary>
    public bool IsIndexed { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyIndexableOption{T}"/> which represents a single value.
    /// </summary>
    /// <param name="singleValue">The single value this <see cref="BitChartLegacyIndexableOption{T}"/> should represent.</param>
    public BitChartLegacyIndexableOption(T? singleValue)
    {
        _singleValue = singleValue != null ? singleValue : throw new ArgumentNullException(nameof(singleValue));
        IsIndexed = false;
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyIndexableOption{T}"/> which represents an array of values.
    /// </summary>
    /// <param name="indexedValues">The array of values this <see cref="BitChartLegacyIndexableOption{T}"/> should represent.</param>
    public BitChartLegacyIndexableOption(T[]? indexedValues)
    {
        _indexedValues = indexedValues ?? throw new ArgumentNullException(nameof(indexedValues));
        IsIndexed = true;
    }

    /// <summary>
    /// Implicitly wraps a single value of <typeparamref name="T"/> to a new instance of <see cref="BitChartLegacyIndexableOption{T}"/>.
    /// </summary>
    /// <param name="singleValue">The single value to wrap</param>
    public static implicit operator BitChartLegacyIndexableOption<T>(T? singleValue)
    {
        CheckIsNotIndexableOption(singleValue?.GetType());

        return new BitChartLegacyIndexableOption<T>(singleValue);
    }

    /// <summary>
    /// Implicitly wraps an array of values of <typeparamref name="T"/> to a new instance of <see cref="BitChartLegacyIndexableOption{T}"/>.
    /// </summary>
    /// <param name="indexedValues">The array of values to wrap</param>
    public static implicit operator BitChartLegacyIndexableOption<T>(T[]? indexedValues)
    {
        CheckIsNotIndexableOption(indexedValues?.GetType().GetElementType());

        return new BitChartLegacyIndexableOption<T>(indexedValues);
    }

    private static void CheckIsNotIndexableOption(Type? type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType) return;
        if (type.GetGenericTypeDefinition() == typeof(BitChartLegacyIndexableOption<>))
            throw new ArgumentException("You cannot use an indexable option inside an indexable option.");
    }

    /// <summary>
    /// Determines whether the specified <see cref="BitChartLegacyIndexableOption{T}"/> instance is considered equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="BitChartLegacyIndexableOption{T}"/> to compare with.</param>
    /// <returns>true if the objects are considered equal; otherwise, false.</returns>
    public bool Equals(BitChartLegacyIndexableOption<T>? other)
    {
        if (IsIndexed != other?.IsIndexed) return false;

        if (IsIndexed)
        {
            if (IndexedValues == other?.IndexedValues) return true;

            if (IndexedValues is null || other is null || other.IndexedValues is null) return false;

            return Enumerable.SequenceEqual(IndexedValues, other.IndexedValues);
        }
        else
        {
            return EqualityComparer<T>.Default.Equals(SingleValue, other.SingleValue);
        }
    }

    /// <summary>
    /// Determines whether the specified object instance is considered equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>true if the objects are considered equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        // an indexable option cannot store null
        if (obj == null) return false;

        if (obj is BitChartLegacyIndexableOption<T> option)
        {
            return Equals(option);
        }
        else
        {
            if (IsIndexed)
            {
                return IndexedValues?.Equals(obj) ?? false;
            }
            else
            {
                return SingleValue?.Equals(obj) ?? false;
            }
        }
    }

    /// <summary>
    /// Returns the hash of the underlying object.
    /// </summary>
    /// <returns>The hash of the underlying object.</returns>
    public override int GetHashCode()
    {
        var hashCode = -506568782;
        hashCode = hashCode * -1521134295 + EqualityComparer<T[]>.Default.GetHashCode(_indexedValues!);
        hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(_singleValue!);
        hashCode = hashCode * -1521134295 + IsIndexed.GetHashCode();
        return hashCode;
    }

    /// <summary>
    /// Determines whether two specified <see cref="BitChartLegacyIndexableOption{T}"/> instances contain the same value.
    /// </summary>
    /// <param name="a">The first <see cref="BitChartLegacyIndexableOption{T}"/> to compare</param>
    /// <param name="b">The second <see cref="BitChartLegacyIndexableOption{T}"/> to compare</param>
    /// <returns>true if the value of a is the same as the value of b; otherwise, false.</returns>
    public static bool operator ==(BitChartLegacyIndexableOption<T> a, BitChartLegacyIndexableOption<T> b) => a.Equals(b);

    /// <summary>
    /// Determines whether two specified <see cref="BitChartLegacyIndexableOption{T}"/> instances contain different values.
    /// </summary>
    /// <param name="a">The first <see cref="BitChartLegacyIndexableOption{T}"/> to compare</param>
    /// <param name="b">The second <see cref="BitChartLegacyIndexableOption{T}"/> to compare</param>
    /// <returns>true if the value of a is different from the value of b; otherwise, false.</returns>
    public static bool operator !=(BitChartLegacyIndexableOption<T> a, BitChartLegacyIndexableOption<T> b) => !(a == b);
}
