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
    public Type ValueType { get; } = valueType ?? value?.GetType() ?? throw new ArgumentNullException(nameof(valueType));



    public BitCascadingValue(object? value, string? name = null) : this(value, name, false) { }
    public BitCascadingValue(object? value, bool isFixed) : this(value, null, isFixed) { }
    public BitCascadingValue(object? value, Type valueType) : this(value, null, false, valueType) { }



    public static BitCascadingValue From<T>(T value, string? name, bool isFixed) => new(value, name, isFixed, typeof(T));



    public static implicit operator BitCascadingValue(int value) => new(value);
    public static implicit operator BitCascadingValue(int? value) => new(value, typeof(int?));

    public static implicit operator BitCascadingValue(bool value) => new(value);
    public static implicit operator BitCascadingValue(bool? value) => new(value, typeof(bool?));
    
    public static implicit operator BitCascadingValue(string value) => new(value);
    public static implicit operator BitCascadingValue(BitDir? value) => new(value, typeof(BitDir?));
    
    public static implicit operator BitCascadingValue(RouteData value) => new(value);
}
