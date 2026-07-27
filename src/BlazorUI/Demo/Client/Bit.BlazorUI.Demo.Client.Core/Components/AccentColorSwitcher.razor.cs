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
/// The accent swatches: one brand color fed to <see cref="BitThemeFactory"/> re-themes the whole app
/// live, which demonstrates the theming system better than describing it could. The accent itself is
/// owned by <see cref="AppAccentColorService"/> - this only renders it and hands clicks over, so
/// every place the switcher appears shows and sets the same one color.
/// </summary>
public partial class AccentColorSwitcher
{
    [Parameter] public AccentColorSwitcherPlacement Placement { get; set; }

    [AutoInject] private AppAccentColorService _accentColorService { get; set; } = default!;

    protected override Task OnInitAsync()
    {
        // Subscribed this early - rather than after the first render, the way a JS-backed
        // notification would have to be - so the restore the layout kicks off cannot land in the gap
        // between rendering the swatches and listening for the accent they mark.
        _accentColorService.AccentChanged += OnAccentChanged;

        return base.OnInitAsync();
    }

    private void OnAccentChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        _accentColorService.AccentChanged -= OnAccentChanged;

        return base.DisposeAsync(disposing);
    }
}
