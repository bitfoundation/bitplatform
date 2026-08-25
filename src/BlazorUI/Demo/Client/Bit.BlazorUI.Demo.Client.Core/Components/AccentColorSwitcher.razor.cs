namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>
/// Where the switcher is being rendered, which decides its size and the widths it shows at. The two
/// chrome placements are complementary halves of one responsive control: exactly one of them is
/// visible at any width, so the app never shows two controls for the same setting.
/// </summary>
public enum AccentColorSwitcherPlacement
{
    /// <summary>In page content - full size, always visible.</summary>
    Content,

    /// <summary>In the app header - compact, and only while the header has room beside the nav links.</summary>
    Header,

    /// <summary>In the nav drawer - shown exactly at the widths where the header's copy is not.</summary>
    NavPanel,
}

/// <summary>
/// The app-chrome shell around <see cref="BitAccentColorSwitcher"/>: one brand color fed to
/// <see cref="BitThemeFactory"/> re-themes the whole app live, which demonstrates the theming system
/// better than describing it could. The component (and its <see cref="BitAccentColorService"/>) owns
/// the accent itself - state, persistence and first paint - so this only decides where the swatches
/// sit and how big they render there.
/// </summary>
public partial class AccentColorSwitcher
{
    [Parameter] public AccentColorSwitcherPlacement Placement { get; set; }
}
