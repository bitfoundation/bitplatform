namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhMenu"/> to its button and panel.
/// Carries open state and the ids used for ARIA wiring.
/// </summary>
public sealed class BitBhMenuContext : IBitBhCloseContext
{
    internal BitBhMenuContext(BitBhMenu menu, bool isOpen, bool disabled, string baseId, string buttonId, string itemsId, BitBhMenubarContext? menubar)
    {
        Menu = menu;
        IsOpen = isOpen;
        Disabled = disabled;
        BaseId = baseId;
        ButtonId = buttonId;
        ItemsId = itemsId;
        Menubar = menubar;
    }

    internal BitBhMenu Menu { get; }

    internal BitBhMenubarContext? Menubar { get; }

    /// <summary>Whether the menu is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the menu is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Base id used to derive button and panel ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the menu trigger button.</summary>
    public string ButtonId { get; }

    /// <summary>Deterministic id of the menu items panel.</summary>
    public string ItemsId { get; }

    /// <summary>Whether the menu opens on hover (<c>OpenOnHover</c>).</summary>
    public bool OpenOnHover => Menu.OpenOnHover;

    /// <summary>Whether it is open because of a hover (its panel takes no focus and closes when the pointer leaves).</summary>
    public bool OpenedByHover => Menu.OpenedByHover;

    /// <summary>Opens the menu.</summary>
    public Task OpenAsync(BitBhMenuFocus focus = BitBhMenuFocus.Container) => Menu.OpenAsync(focus);

    /// <summary>Closes the menu.</summary>
    public Task CloseAsync() => Menu.CloseAsync();

    internal Task ToggleAsync(BitBhMenuFocus focus) => Menu.ToggleAsync(focus);
}
