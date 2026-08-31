using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The cascading value to be provided using the <see cref="BitCascadingValueProvider"/> component.
/// </summary>
public class BitCascadingValue
{
    private object? _value;
    private string? _name;



    /// <summary>
    /// Creates a new cascading value.
    /// </summary>
    /// <param name="value">The value to be provided.</param>
    /// <param name="name">The optional name of the cascading value.</param>
    /// <param name="isFixed">Determines that the value will not change.</param>
    /// <param name="valueType">
    /// The type to be used as the TValue of the underlying CascadingValue component.
    /// When not provided, the runtime type of the <paramref name="value"/> is used, so it must be
    /// provided whenever the value is null or its static type differs from its runtime type.
    /// </param>
    public BitCascadingValue(object? value, string? name, bool isFixed, Type? valueType = null)
    {
        ValueType = valueType
                 ?? value?.GetType()
                 ?? throw new ArgumentNullException(nameof(valueType), "Either the value must be non-null or the valueType must be explicitly provided.");

        ValidateValue(value, ValueType);

        _value = value;
        _name = NormalizeName(name);
        IsFixed = isFixed;
    }

    public BitCascadingValue(object? value, string? name = null) : this(value, name, false) { }
    public BitCascadingValue(object? value, bool isFixed) : this(value, null, isFixed) { }
    public BitCascadingValue(object? value, Type valueType) : this(value, null, false, valueType) { }
    public BitCascadingValue(object? value, string name, Type valueType) : this(value, name, false, valueType) { }



    /// <summary>
    /// The value to be provided.
    /// </summary>
    public object? Value
    {
        get => _value;
        set
        {
            ValidateValue(value, ValueType);
            _value = value;
        }
    }

    /// <summary>
    /// The optional name of the cascading value. An empty or white-space name is treated as no name at all.
    /// </summary>
    public string? Name
    {
        get => _name;
        set => _name = NormalizeName(value);
    }

    /// <summary>
    /// If true, indicates that <see cref="Value"/> will not change.
    /// </summary>
    public bool IsFixed { get; set; }

    /// <summary>
    /// Determines whether this cascading value is provided to the children. A disabled value is skipped
    /// by the <see cref="BitCascadingValueProvider"/> as if it was never added, which lets an outer or a
    /// root level cascading value of the same type or name show through.
    /// Toggling it changes the shape of the rendered tree, so the child content is re-created just like
    /// it would be when a CascadingValue component is wrapped in a conditional block.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The actual type of the value to be used as the TValue of the CascadingValue component.
    /// </summary>
    public Type ValueType { get; }



    /// <summary>
    /// Creates a cascading value whose ValueType is the static type of <typeparamref name="T"/>, which is
    /// the safe way of cascading null values, nullable value types, interfaces and base types.
    /// </summary>
    public static BitCascadingValue From<T>(T value, string? name, bool isFixed) => new(value, name, isFixed, typeof(T));

    /// <summary>
    /// Creates a cascading value whose ValueType is the static type of <typeparamref name="T"/>.
    /// </summary>
    public static BitCascadingValue From<T>(T value) => new(value, null, false, typeof(T));

    /// <summary>
    /// Creates a named cascading value whose ValueType is the static type of <typeparamref name="T"/>.
    /// </summary>
    public static BitCascadingValue From<T>(T value, string? name) => new(value, name, false, typeof(T));

    /// <summary>
    /// Creates a cascading value whose ValueType is the static type of <typeparamref name="T"/>.
    /// </summary>
    public static BitCascadingValue From<T>(T value, bool isFixed) => new(value, null, isFixed, typeof(T));

    /// <summary>
    /// Creates a fixed (IsFixed) cascading value whose ValueType is the static type of <typeparamref name="T"/>.
    /// Fixed values never subscribe their consumers for change notifications, so they are the cheapest way
    /// of cascading a value that never changes.
    /// </summary>
    public static BitCascadingValue Fixed<T>(T value, string? name = null) => new(value, name, true, typeof(T));



    public override string ToString() => $"{(Name is null ? string.Empty : $"{Name}: ")}{ValueType.Name} = {Value ?? "null"}";



    private static string? NormalizeName(string? name) => string.IsNullOrWhiteSpace(name) ? null : name;

    private static void ValidateValue(object? value, Type valueType)
    {
        if (value is null)
        {
            if (valueType.IsValueType && Nullable.GetUnderlyingType(valueType) is null)
            {
                throw new ArgumentException($"A null value cannot be cascaded as the non-nullable value type '{valueType}'. Provide a nullable valueType instead.", nameof(value));
            }

            return;
        }

        var type = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (type.IsInstanceOfType(value) is false)
        {
            throw new ArgumentException($"The provided value of type '{value.GetType()}' is not assignable to the cascading value type '{valueType}'.", nameof(value));
        }
    }



    public static implicit operator BitCascadingValue(bool value) => new(value);
    public static implicit operator BitCascadingValue((bool value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(bool? value) => new(value, typeof(bool?));
    public static implicit operator BitCascadingValue((bool? value, string name) tuple) => new(tuple.value, tuple.name, typeof(bool?));

    public static implicit operator BitCascadingValue(byte value) => new(value);
    public static implicit operator BitCascadingValue((byte value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(byte? value) => new(value, typeof(byte?));
    public static implicit operator BitCascadingValue((byte? value, string name) tuple) => new(tuple.value, tuple.name, typeof(byte?));

    public static implicit operator BitCascadingValue(sbyte value) => new(value);
    public static implicit operator BitCascadingValue((sbyte value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(sbyte? value) => new(value, typeof(sbyte?));
    public static implicit operator BitCascadingValue((sbyte? value, string name) tuple) => new(tuple.value, tuple.name, typeof(sbyte?));

    public static implicit operator BitCascadingValue(short value) => new(value);
    public static implicit operator BitCascadingValue((short value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(short? value) => new(value, typeof(short?));
    public static implicit operator BitCascadingValue((short? value, string name) tuple) => new(tuple.value, tuple.name, typeof(short?));

    public static implicit operator BitCascadingValue(ushort value) => new(value);
    public static implicit operator BitCascadingValue((ushort value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(ushort? value) => new(value, typeof(ushort?));
    public static implicit operator BitCascadingValue((ushort? value, string name) tuple) => new(tuple.value, tuple.name, typeof(ushort?));

    public static implicit operator BitCascadingValue(int value) => new(value);
    public static implicit operator BitCascadingValue((int value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(int? value) => new(value, typeof(int?));
    public static implicit operator BitCascadingValue((int? value, string name) tuple) => new(tuple.value, tuple.name, typeof(int?));

    public static implicit operator BitCascadingValue(uint value) => new(value);
    public static implicit operator BitCascadingValue((uint value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(uint? value) => new(value, typeof(uint?));
    public static implicit operator BitCascadingValue((uint? value, string name) tuple) => new(tuple.value, tuple.name, typeof(uint?));

    public static implicit operator BitCascadingValue(long value) => new(value);
    public static implicit operator BitCascadingValue((long value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(long? value) => new(value, typeof(long?));
    public static implicit operator BitCascadingValue((long? value, string name) tuple) => new(tuple.value, tuple.name, typeof(long?));

    public static implicit operator BitCascadingValue(ulong value) => new(value);
    public static implicit operator BitCascadingValue((ulong value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(ulong? value) => new(value, typeof(ulong?));
    public static implicit operator BitCascadingValue((ulong? value, string name) tuple) => new(tuple.value, tuple.name, typeof(ulong?));

    public static implicit operator BitCascadingValue(nint value) => new(value);
    public static implicit operator BitCascadingValue((nint value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(nint? value) => new(value, typeof(nint?));
    public static implicit operator BitCascadingValue((nint? value, string name) tuple) => new(tuple.value, tuple.name, typeof(nint?));

    public static implicit operator BitCascadingValue(nuint value) => new(value);
    public static implicit operator BitCascadingValue((nuint value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(nuint? value) => new(value, typeof(nuint?));
    public static implicit operator BitCascadingValue((nuint? value, string name) tuple) => new(tuple.value, tuple.name, typeof(nuint?));

    public static implicit operator BitCascadingValue(float value) => new(value);
    public static implicit operator BitCascadingValue((float value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(float? value) => new(value, typeof(float?));
    public static implicit operator BitCascadingValue((float? value, string name) tuple) => new(tuple.value, tuple.name, typeof(float?));

    public static implicit operator BitCascadingValue(double value) => new(value);
    public static implicit operator BitCascadingValue((double value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(double? value) => new(value, typeof(double?));
    public static implicit operator BitCascadingValue((double? value, string name) tuple) => new(tuple.value, tuple.name, typeof(double?));

    public static implicit operator BitCascadingValue(decimal value) => new(value);
    public static implicit operator BitCascadingValue((decimal value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(decimal? value) => new(value, typeof(decimal?));
    public static implicit operator BitCascadingValue((decimal? value, string name) tuple) => new(tuple.value, tuple.name, typeof(decimal?));

    public static implicit operator BitCascadingValue(char value) => new(value);
    public static implicit operator BitCascadingValue((char value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(char? value) => new(value, typeof(char?));
    public static implicit operator BitCascadingValue((char? value, string name) tuple) => new(tuple.value, tuple.name, typeof(char?));

    public static implicit operator BitCascadingValue(Guid value) => new(value);
    public static implicit operator BitCascadingValue((Guid value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(Guid? value) => new(value, typeof(Guid?));
    public static implicit operator BitCascadingValue((Guid? value, string name) tuple) => new(tuple.value, tuple.name, typeof(Guid?));

    public static implicit operator BitCascadingValue(DateTime value) => new(value);
    public static implicit operator BitCascadingValue((DateTime value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(DateTime? value) => new(value, typeof(DateTime?));
    public static implicit operator BitCascadingValue((DateTime? value, string name) tuple) => new(tuple.value, tuple.name, typeof(DateTime?));

    public static implicit operator BitCascadingValue(DateOnly value) => new(value);
    public static implicit operator BitCascadingValue((DateOnly value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(DateOnly? value) => new(value, typeof(DateOnly?));
    public static implicit operator BitCascadingValue((DateOnly? value, string name) tuple) => new(tuple.value, tuple.name, typeof(DateOnly?));

    public static implicit operator BitCascadingValue(TimeOnly value) => new(value);
    public static implicit operator BitCascadingValue((TimeOnly value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(TimeOnly? value) => new(value, typeof(TimeOnly?));
    public static implicit operator BitCascadingValue((TimeOnly? value, string name) tuple) => new(tuple.value, tuple.name, typeof(TimeOnly?));

    public static implicit operator BitCascadingValue(DateTimeOffset value) => new(value);
    public static implicit operator BitCascadingValue((DateTimeOffset value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(DateTimeOffset? value) => new(value, typeof(DateTimeOffset?));
    public static implicit operator BitCascadingValue((DateTimeOffset? value, string name) tuple) => new(tuple.value, tuple.name, typeof(DateTimeOffset?));

    public static implicit operator BitCascadingValue(TimeSpan value) => new(value);
    public static implicit operator BitCascadingValue((TimeSpan value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(TimeSpan? value) => new(value, typeof(TimeSpan?));
    public static implicit operator BitCascadingValue((TimeSpan? value, string name) tuple) => new(tuple.value, tuple.name, typeof(TimeSpan?));

    public static implicit operator BitCascadingValue(string? value) => new(value, typeof(string));
    public static implicit operator BitCascadingValue((string? value, string name) tuple) => new(tuple.value, tuple.name, typeof(string));

    public static implicit operator BitCascadingValue(BitDir value) => new(value);
    public static implicit operator BitCascadingValue((BitDir value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(BitDir? value) => new(value, typeof(BitDir?));
    public static implicit operator BitCascadingValue((BitDir? value, string name) tuple) => new(tuple.value, tuple.name, typeof(BitDir?));

    public static implicit operator BitCascadingValue(RouteData? value) => new(value, typeof(RouteData));
    public static implicit operator BitCascadingValue((RouteData? value, string name) tuple) => new(tuple.value, tuple.name, typeof(RouteData));

    public static implicit operator BitCascadingValue(Uri? value) => new(value, typeof(Uri));
    public static implicit operator BitCascadingValue((Uri? value, string name) tuple) => new(tuple.value, tuple.name, typeof(Uri));

    public static implicit operator BitCascadingValue(CultureInfo? value) => new(value, typeof(CultureInfo));
    public static implicit operator BitCascadingValue((CultureInfo? value, string name) tuple) => new(tuple.value, tuple.name, typeof(CultureInfo));

    public static implicit operator BitCascadingValue(TimeZoneInfo? value) => new(value, typeof(TimeZoneInfo));
    public static implicit operator BitCascadingValue((TimeZoneInfo? value, string name) tuple) => new(tuple.value, tuple.name, typeof(TimeZoneInfo));
}
