namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class HeroSection
{
    [AutoInject] private BitThemeManager _themeManager { get; set; } = default!;
    [AutoInject] private BitThemeNotifications _themeNotifications { get; set; } = default!;


    // The "Make it yours" swatches: one brand color fed to BitThemeFactory re-themes the whole
    // landing page live, which demonstrates the theming system better than describing it could.
    private static readonly (string Name, string Hex)[] _accents =
    [
        ("Blue", BitAccentColorPresets.Blue),
        ("Purple", BitAccentColorPresets.Purple),
        ("Green", BitAccentColorPresets.Green),
        ("Orange", BitAccentColorPresets.Orange),
        ("Teal", BitAccentColorPresets.Teal),
        ("Rose", BitAccentColorPresets.Rose),
    ];

    private string _activeAccent = BitAccentColorPresets.Blue;

    private List<BitDropdownItem<string>> _productDropdownItems = default!;
    private string _selectedColor = "#0065EF";
    private bool _isToggleChecked = true;
    private bool _isToggleUnChecked = false;
    private string? _pivotSelectedKey = "Overview";

    protected override Task OnAfterFirstRenderAsync()
    {
        // Re-derives the accent overlay for the new scheme when the header's dark/light toggle
        // fires, otherwise a light-scheme primary would linger on the dark palette (and vice
        // versa). Subscribing starts the JS-side notifier registration, so it has to wait for
        // interactivity rather than run during prerendering.
        _themeNotifications.ThemeChanged += OnThemeChanged;

        return base.OnAfterFirstRenderAsync();
    }

    protected override Task OnInitAsync()
    {
        _productDropdownItems = new()
        {
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Fruits"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            },
            new()
            {
                ItemType = BitDropdownItemType.Header,
                Text = "Vegetables"
            },
            new()
            {
                ItemType = BitDropdownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            }
        };

        return base.OnInitAsync();
    }

    private async Task ApplyAccentAsync(string hex)
    {
        _activeAccent = hex;

        // Blue is the packaged palette's own primary, so clearing the overrides is the exact reset.
        if (hex is BitAccentColorPresets.Blue)
        {
            await _themeManager.ClearBitThemeOverridesAsync();
            return;
        }

        await ApplyAccentForThemeAsync(hex, await _themeManager.GetCurrentThemeAsync());
    }

    private async Task ApplyAccentForThemeAsync(string hex, string? themeName)
    {
        var isDark = themeName?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;
        var theme = isDark ? BitThemeFactory.CreateDarkTheme(hex) : BitThemeFactory.CreateLightTheme(hex);
        await _themeManager.ApplyBitThemeAsync(theme);
    }

    private void OnThemeChanged(object? sender, BitThemeChangedEventArgs e)
    {
        if (_activeAccent is BitAccentColorPresets.Blue) return;

        _ = InvokeAsync(WrapHandled(() => ApplyAccentForThemeAsync(_activeAccent, e.NewTheme)));
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        _themeNotifications.ThemeChanged -= OnThemeChanged;

        return base.DisposeAsync(disposing);
    }
}
