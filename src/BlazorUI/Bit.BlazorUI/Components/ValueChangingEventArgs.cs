namespace Bit.BlazorUI;
internal class ValueChangingEventArgs<T> : EventArgs
{
    public T? Value { get; set; }
    public bool ShouldChange { get; set; } = true;
}
