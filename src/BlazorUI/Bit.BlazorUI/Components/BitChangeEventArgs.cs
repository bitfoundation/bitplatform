namespace Bit.BlazorUI;

public class BitChangeEventArgs<T>(T? oldValue, T? newValue)
{
    public T? OldValue { get; set; } = oldValue;
    public T? NewValue { get; set; } = newValue;
}
