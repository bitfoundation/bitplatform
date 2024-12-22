namespace Bit.BlazorUI;

/// <summary>
/// The cascading value to be provided using the <see cref="BitCascadingValueProvider"/> component.
/// </summary>
public class BitCascadingValue
{
    /// <summary>
    /// The optional name of the cascading value.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The value to be provided.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// If true, indicates that <see cref="Value"/> will not change.
    /// </summary>
    public bool IsFixed { get; set; }



    public BitCascadingValue() { }
    public BitCascadingValue(object? value, string? name = null) : this(value, name, false) { }
    public BitCascadingValue(object? value, bool isFixed) : this(value, null, isFixed) { }
    public BitCascadingValue(object? value, string? name, bool isFixed)
    {
        Value = value;
        Name = name;
        IsFixed = isFixed;
    }



    public static implicit operator BitCascadingValue(int value) => new(value);
    public static implicit operator BitCascadingValue(int? value) => new(value);
    public static implicit operator BitCascadingValue(bool value) => new(value);
    public static implicit operator BitCascadingValue(bool? value) => new(value);
    public static implicit operator BitCascadingValue(string value) => new(value);
    public static implicit operator BitCascadingValue(BitDir? value) => new(value);
    public static implicit operator BitCascadingValue(RouteData value) => new(value);
}
