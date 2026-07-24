using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Iconography;

public partial class IconographyPage
{
    private sealed record IconInfo(string FieldName, string Value)
    {
        public string ConstantReference => $"BitIconName.{FieldName}";

        public string CssClass => $"bit-icon bit-icon--{Value}";

        public string RazorIconName => $"IconName=\"@BitIconName.{FieldName}\"";

        public string RazorIconInfo => $"Icon=\"@BitIconInfo.Bit(\"{Value}\")\"";
    }



    private const double IconPanelSize = 400;

    private const string COPY_KEY_NAME = "name";

    private static readonly (string Label, BitColor Value)[] previewColors =
    [
        ("Primary", BitColor.Primary),
        ("Secondary", BitColor.Secondary),
        ("Tertiary", BitColor.Tertiary),
        ("Info", BitColor.Info),
        ("Success", BitColor.Success),
        ("Warning", BitColor.Warning),
        ("Error", BitColor.Error),
    ];

    private List<IconInfo> allIcons = default!;
    private List<IconInfo> filteredIcons = default!;
    private IconInfo? selectedIcon;
    private bool isIconPanelOpen;
    private Dictionary<string, string>? iconGlyphs;
    private string? copyFeedbackKey;



    [AutoInject] private IJSRuntime _js = default!;

    [AutoInject] private HttpClient _http = default!;



    protected override void OnInitialized()
    {
        allIcons = [.. typeof(BitIconName).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(m => new IconInfo(m.Name, m.GetValue(null)?.ToString() ?? string.Empty))
            .Where(i => string.IsNullOrEmpty(i.Value) is false)
            .OrderBy(i => i.Value, StringComparer.OrdinalIgnoreCase)];

        HandleClear();
        base.OnInitialized();
    }



    private void HandleClear()
    {
        filteredIcons = allIcons;
    }

    private void HandleChange(string text)
    {
        HandleClear();
        if (string.IsNullOrEmpty(text)) return;

        filteredIcons = allIcons.FindAll(icon =>
            icon.Value.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
            icon.FieldName.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task OpenIconPanel(IconInfo icon)
    {
        selectedIcon = icon;
        isIconPanelOpen = true;
        copyFeedbackKey = null;
        await EnsureGlyphsLoadedAsync();
    }

    private async Task CloseIconPanel()
    {
        isIconPanelOpen = false;
        StateHasChanged();

        await Task.Delay(200);

        if (isIconPanelOpen is false)
        {
            selectedIcon = null;
            StateHasChanged();
        }
    }

    private Task HandleIconPanelDismissed(MouseEventArgs _)
    {
        return CloseIconPanel();
    }

    private string? GetGlyphCode(IconInfo icon)
    {
        if (iconGlyphs is null) return null;

        if (iconGlyphs.TryGetValue(icon.Value, out var glyph) is false || string.IsNullOrEmpty(glyph)) return null;

        if (glyph[0] == '\\' && int.TryParse(glyph[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var escapedCodePoint))
        {
            return $"\\{escapedCodePoint:X4}";
        }

        return $"\\{char.ConvertToUtf32(glyph, 0):X4}";
    }

    private async Task EnsureGlyphsLoadedAsync()
    {
        if (iconGlyphs is not null) return;

        try
        {
            var css = await _http.GetStringAsync("_content/Bit.BlazorUI.Icons/styles/bit.blazorui.icons.css");
            iconGlyphs = Regex.Matches(css, @"\.bit-icon--([^:{]+)::before\{content:""([^""]+)""\}")
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        }
        catch
        {
            iconGlyphs = null;
        }
    }

    private Task CopyIconName() => CopyToClipboard(selectedIcon!.Value, COPY_KEY_NAME);

    private Task HandleDetailCopy((string Text, string Key) args) => CopyToClipboard(args.Text, args.Key);

    private async Task CopyToClipboard(string text, string feedbackKey)
    {
        await _js.CopyToClipboard(text);
        copyFeedbackKey = feedbackKey;
        StateHasChanged();

        await Task.Delay(1500);

        if (copyFeedbackKey == feedbackKey)
        {
            copyFeedbackKey = null;
            StateHasChanged();
        }
    }
}
