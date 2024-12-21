namespace Bit.BlazorUI;

public class BitCascadingValue
{
    public string? Name { get; set; }
    public object? Value { get; set; }

    public BitCascadingValue() { }

    public BitCascadingValue(object? value, string? name = null)
    {
        Name = name;
        Value = value;
    }

    public static implicit operator BitCascadingValue(int value) => new(value);
    public static implicit operator BitCascadingValue(int? value) => new(value);
    public static implicit operator BitCascadingValue(bool value) => new(value);
    public static implicit operator BitCascadingValue(bool? value) => new(value);
    public static implicit operator BitCascadingValue(string value) => new(value);
    public static implicit operator BitCascadingValue(BitDir? value) => new(value);
    public static implicit operator BitCascadingValue(RouteData value) => new(value);
}
