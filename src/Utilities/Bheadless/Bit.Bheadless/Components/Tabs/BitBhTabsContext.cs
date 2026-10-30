namespace Bit.Bheadless;

/// <summary>The orientation of an <see cref="BitBhTabGroup"/>.</summary>
public enum BitBhTabsOrientation
{
    /// <summary>Tabs flow horizontally; Left / Right arrow keys navigate.</summary>
    Horizontal,

    /// <summary>Tabs flow vertically; Up / Down arrow keys navigate.</summary>
    Vertical,
}

/// <summary>
/// Cascading context provided by <see cref="BitBhTabGroup"/> to its tab list, tabs and panels.
/// </summary>
public sealed class BitBhTabsContext
{
    internal BitBhTabsContext(
        BitBhTabGroup group,
        int selectedIndex,
        bool disabled,
        BitBhOrientation orientation,
        bool manual,
        string baseId,
        BitBhCollection<BitBhTab> tabs,
        BitBhCollection<BitBhTabPanel> panels)
    {
        Group = group;
        SelectedIndex = selectedIndex;
        Disabled = disabled;
        LayoutOrientation = orientation;
        Manual = manual;
        BaseId = baseId;
        Tabs = tabs;
        Panels = panels;
    }

    internal BitBhTabGroup Group { get; }
    internal BitBhCollection<BitBhTab> Tabs { get; }
    internal BitBhCollection<BitBhTabPanel> Panels { get; }

    /// <summary>The selected index (index mode).</summary>
    public int SelectedIndex { get; }

    /// <summary>Whether the whole group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>The orientation.</summary>
    public BitBhOrientation LayoutOrientation { get; }

    /// <summary>The orientation (legacy enum).</summary>
    public BitBhTabsOrientation Orientation => LayoutOrientation == BitBhOrientation.Vertical ? BitBhTabsOrientation.Vertical : BitBhTabsOrientation.Horizontal;

    /// <summary>Whether activation is manual.</summary>
    public bool Manual { get; }

    /// <summary>The group's base id.</summary>
    public string BaseId { get; }
}

/// <summary>Render-fragment context exposed by <see cref="BitBhTab"/>.</summary>
public sealed record BitBhTabRenderContext
{
    /// <summary>Whether this tab is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this tab (or the group) is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>Render-fragment context exposed by <see cref="BitBhTabPanel"/>.</summary>
public sealed record BitBhTabPanelRenderContext
{
    /// <summary>Whether this panel is the one shown.</summary>
    public required bool IsSelected { get; init; }
}
