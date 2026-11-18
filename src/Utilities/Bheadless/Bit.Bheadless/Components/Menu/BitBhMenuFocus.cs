namespace Bit.Bheadless;

/// <summary>How a menu should place its highlight when it opens.</summary>
public enum BitBhMenuFocus
{
    /// <summary>Don't move focus into the menu (pointer-opened submenus).</summary>
    None,

    /// <summary>Focus the menu without highlighting an item (pointer-opened menus).</summary>
    Container,

    /// <summary>Highlight the first enabled item (Enter / Space / ArrowDown).</summary>
    First,

    /// <summary>Highlight the last enabled item (ArrowUp).</summary>
    Last,
}
