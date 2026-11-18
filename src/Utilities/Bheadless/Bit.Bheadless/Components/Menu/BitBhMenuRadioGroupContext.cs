namespace Bit.Bheadless;

/// <summary>Cascading context of a <see cref="BitBhMenuRadioGroup"/>.</summary>
public sealed class BitBhMenuRadioGroupContext
{
    private readonly Func<string, Task> _select;

    internal BitBhMenuRadioGroupContext(string? value, Func<string, Task> select)
    {
        Value = value;
        _select = select;
    }

    /// <summary>The checked value.</summary>
    public string? Value { get; }

    internal Task SelectAsync(string value) => _select(value);
}
