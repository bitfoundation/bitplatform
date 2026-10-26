namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Cascading context provided by <see cref="BitBhPopover"/> to its descendant parts
/// (<see cref="BitBhPopoverButton"/>, <see cref="BitBhPopoverPanel"/>,
/// <see cref="BitBhPopoverBackdrop"/>). Carries open state, ARIA wiring ids, and
/// the open/close callbacks.
/// </summary>
public sealed class BitBhPopoverContext : IBitBhCloseContext
{
    private readonly Func<Task> _openAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Func<Task> _toggleAsync;
    private readonly Func<BitBhDismissReason, Task> _dismissAsync;
    private readonly Action<bool> _registerButton;

    internal BitBhPopoverContext(
        bool isOpen,
        bool modal,
        string baseId,
        bool hasButton,
        Func<Task> openAsync,
        Func<Task> closeAsync,
        Func<Task> toggleAsync,
        Func<BitBhDismissReason, Task> dismissAsync,
        Action<bool> registerButton)
    {
        IsOpen = isOpen;
        Modal = modal;
        BaseId = baseId;
        HasButton = hasButton;
        _registerButton = registerButton;
        _openAsync = openAsync;
        _closeAsync = closeAsync;
        _toggleAsync = toggleAsync;
        _dismissAsync = dismissAsync;
    }

    /// <summary>Whether the popover panel is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the popover is modal.</summary>
    public bool Modal { get; }

    /// <summary>Base id used to derive button and panel ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the popover button.</summary>
    public string ButtonId => $"{BaseId}-button";

    /// <summary>Whether a <see cref="BitBhPopoverButton"/> (outside the panel) is rendered.</summary>
    public bool HasButton { get; }

    internal void RegisterButton(bool present) => _registerButton(present);

    /// <summary>Deterministic id of the popover panel.</summary>
    public string PanelId => $"{BaseId}-panel";

    /// <summary>Opens the popover.</summary>
    public Task OpenAsync() => _openAsync();

    /// <summary>Closes the popover.</summary>
    public Task CloseAsync() => _closeAsync();

    /// <summary>Toggles the popover open or closed.</summary>
    public Task ToggleAsync() => _toggleAsync();

    /// <summary>Requests dismissal (respects <c>OnDismissRequest</c>).</summary>
    public Task DismissAsync(BitBhDismissReason reason) => _dismissAsync(reason);
}

/// <summary>
/// Render-fragment context exposed by <see cref="BitBhPopoverButton"/> and
/// <see cref="BitBhPopoverPanel"/> for state-driven rendering.
/// </summary>
public sealed record BitBhPopoverRenderContext
{
    /// <summary>Whether the popover panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the popover. Useful from inside the panel (e.g. a "Close" link).</summary>
    public required Action Close { get; init; }
}

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
