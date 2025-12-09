namespace Bit.BlazorUI;

/// <summary>
/// The cascading value to be provided using the <see cref="BitCascadingValueProvider"/> component.
/// </summary>
public class BitCascadingValue(object? value, string? name, bool isFixed, Type? valueType = null)
{
    /// <summary>
    /// The optional name of the cascading value.
    /// </summary>
    public string? Name { get; set; } = name;

    /// <summary>
    /// The value to be provided.
    /// </summary>
    public object? Value { get; set; } = value;

    /// <summary>
    /// If true, indicates that <see cref="Value"/> will not change.
    /// </summary>
    public bool IsFixed { get; set; } = isFixed;

    /// <summary>
    /// The actual type of the value to be used as the TValue of the CascadingValue component.
    /// </summary>
    public Type ValueType { get; } = valueType ?? value?.GetType() ?? throw new ArgumentNullException("Either value must be non-null or valueType must be explicitly provided.", nameof(valueType));



    public BitCascadingValue(object? value, string? name = null) : this(value, name, false) { }
    public BitCascadingValue(object? value, bool isFixed) : this(value, null, isFixed) { }
    public BitCascadingValue(object? value, Type valueType) : this(value, null, false, valueType) { }
    public BitCascadingValue(object? value, string name, Type valueType) : this(value, name, false, valueType) { }



    public static BitCascadingValue From<T>(T value, string? name, bool isFixed) => new(value, name, isFixed, typeof(T));



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
}
