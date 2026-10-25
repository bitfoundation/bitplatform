namespace Bit.BlazorUI;

/// <summary>
/// The screen width below which a <see cref="BitNavPanel{TItem}"/> stops being a column of the page and turns
/// into an off-canvas drawer.
/// </summary>
public enum BitNavPanelBreakpoint
{
    /// <summary>
    /// The panel is never a drawer: it stays a column of the page on every screen.
    /// </summary>
    Never,

    /// <summary>
    /// The panel is a drawer below the sm breakpoint (600px).
    /// </summary>
    Sm,

    /// <summary>
    /// The panel is a drawer below the md breakpoint (960px). This is the default.
    /// </summary>
    Md,

    /// <summary>
    /// The panel is a drawer below the lg breakpoint (1280px).
    /// </summary>
    Lg,

    /// <summary>
    /// The panel is a drawer below the xl breakpoint (1920px).
    /// </summary>
    Xl,

    /// <summary>
    /// The panel is a drawer on every screen, opened and closed through <see cref="BitNavPanel{TItem}.IsOpen"/>.
    /// </summary>
    Always
}
