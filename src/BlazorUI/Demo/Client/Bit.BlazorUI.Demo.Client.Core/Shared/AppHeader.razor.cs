using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class AppHeader
{
    private string _currentUrl = string.Empty;

    private string? _designSystem = "fluent";

    private static readonly List<BitDropdownItem<string>> _designSystems =
    [
        new() { Text = "Fluent", Value = "fluent" },
        new() { Text = "Material", Value = "material" },
        new() { Text = "Cupertino", Value = "cupertino" },
    ];


    [AutoInject] private BitThemeManager _bitThemeManager { get; set; } = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;


    [Parameter] public bool IsHomePage { get; set; }
    [Parameter] public EventCallback OnToggleNavPanel { get; set; }

    /// <summary>
    /// The persisted theme preference (the bit-theme-preference cookie), cascaded by the server host
    /// while prerendering; null everywhere else.
    /// </summary>
    [CascadingParameter(Name = "PersistedTheme")] public string? PersistedTheme { get; set; }


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // Prerendering has no JS runtime to ask, so the server reads the persisted theme from its
        // cookie and cascades it here; the value is then persisted into the prerendered state so the
        // interactive client comes up with the same selection instead of flashing "Fluent" first.
        _designSystem = await PrerenderStateService.GetValue("AppHeader.DesignSystem", () => Task.FromResult<string?>(GetDesignSystem(PersistedTheme)));
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        // The theme script restores the persisted theme on its own, so after a refresh the page is
        // already painted in the right design system; only this dropdown would still show its
        // hard-coded default. Read the applied theme back (JS interop, hence after the first render)
        // and select the design system it belongs to.
        var current = await _bitThemeManager.GetCurrentThemeAsync();
        var system = GetDesignSystem(current);

        if (system != _designSystem)
        {
            _designSystem = system;
            StateHasChanged();
        }
    }

    private static string GetDesignSystem(string? theme)
    {
        if (theme is null) return "fluent";

        return _designSystems
                .Select(i => i.Value!)
                .FirstOrDefault(s => s is not "fluent" && theme.StartsWith(s, StringComparison.OrdinalIgnoreCase))
               ?? "fluent";
    }

    private async Task ToggleNavMenu()
    {
        await OnToggleNavPanel.InvokeAsync();
    }

    private async Task ToggleTheme()
    {
        var current = await _bitThemeManager.GetCurrentThemeAsync();

        // The Material/Cupertino presets are not the configured dark/light pair the JS toggle flips
        // between, so flip their scheme suffix here instead of falling back to the Fluent pair.
        if (current is not null && (current.StartsWith("material") || current.StartsWith("cupertino")))
        {
            var isDark = current.EndsWith("-dark");
            var system = current.Split('-')[0];
            await _bitThemeManager.SetThemeAsync($"{system}-{(isDark ? "light" : "dark")}");
            await _bitDeviceCoordinator.ApplyTheme(isDark is false);
            return;
        }

        await _bitDeviceCoordinator.ApplyTheme(await _bitThemeManager.ToggleDarkLightAsync() == "dark");
    }

    private async Task OnDesignSystemChanged(BitDropdownItem<string> item)
    {
        var system = item.Value;
        if (system is null) return;

        var current = await _bitThemeManager.GetCurrentThemeAsync();
        var isDark = current?.Contains("dark") is true;

        var target = system is "fluent"
            ? (isDark ? BitThemePresets.Dark : BitThemePresets.Light)
            : $"{system}-{(isDark ? "dark" : "light")}";

        await _bitThemeManager.SetThemeAsync(target);
    }
}
