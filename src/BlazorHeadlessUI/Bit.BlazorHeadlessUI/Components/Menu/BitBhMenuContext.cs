namespace Bit.BlazorHeadlessUI;

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

    /// <summary>Opens the menu.</summary>
    public Task OpenAsync(BitBhMenuFocus focus = BitBhMenuFocus.Container) => Menu.OpenAsync(focus);

    /// <summary>Closes the menu.</summary>
    public Task CloseAsync() => Menu.CloseAsync();

    internal Task ToggleAsync(BitBhMenuFocus focus) => Menu.ToggleAsync(focus);
}

/// <summary>Render-fragment context exposed by <see cref="BitBhMenuButton"/>.</summary>
public sealed record BitBhMenuButtonRenderContext
{
    /// <summary>Whether the menu panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the menu is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by menu items.</summary>
public sealed record BitBhMenuItemRenderContext
{
    /// <summary>Whether this item is currently the highlighted item.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this item is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Whether the item is checked (checkbox and radio items; false otherwise).</summary>
    public bool Checked { get; init; }

    /// <summary>Whether the item's submenu is open (submenu triggers; false otherwise).</summary>
    public bool IsOpen { get; init; }
}
