namespace Bit.Bheadless;

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
