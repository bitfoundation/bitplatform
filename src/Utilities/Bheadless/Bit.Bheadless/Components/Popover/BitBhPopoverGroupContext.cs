namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhPopoverGroup"/> to coordinate
/// mutual exclusion between sibling popovers.
/// </summary>
public sealed class BitBhPopoverGroupContext
{
    private readonly Action<BitBhPopover> _register;
    private readonly Action<BitBhPopover> _unregister;
    private readonly Func<BitBhPopover, Task> _closeOthersAsync;

    internal BitBhPopoverGroupContext(
        Action<BitBhPopover> register,
        Action<BitBhPopover> unregister,
        Func<BitBhPopover, Task> closeOthersAsync)
    {
        _register = register;
        _unregister = unregister;
        _closeOthersAsync = closeOthersAsync;
    }

    internal void Register(BitBhPopover popover) => _register(popover);
    internal void Unregister(BitBhPopover popover) => _unregister(popover);
    internal Task CloseOthersAsync(BitBhPopover except) => _closeOthersAsync(except);
}
