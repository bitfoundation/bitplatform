using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Iconography;

public partial class IconographyPage : IAsyncDisposable
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

    private const string GRID_ELEMENT_ID = "iconography-grid";

    // The geometry of one cell of the icon grid, in px, duplicating what the stylesheet lays out -
    // and it has to stay a duplicate. The virtualizer places its rows at absolute pixel offsets, so
    // the row height it is handed must be exactly the height a row occupies, and the column count
    // derived from these must be exactly how many cells a row fits, or the rows drift apart or
    // overlap. The matching $icon-cell-* values live in IconographyPage.razor.scss.
    private const int ICON_CELL_WIDTH = 88;
    private const int ICON_CELL_GAP = 8;
    private const int ICON_ROW_HEIGHT = 76;

    // How wide the grid is assumed to be until it has been measured. Nothing measures anything while
    // the page is being prerendered - OnAfterRenderAsync never runs there - so a count of zero would
    // chunk the icons into no rows at all and ship a page with not one icon on it, to a crawler and
    // to a reader whose JS has not arrived yet. A desktop-ish guess instead: the first client frame
    // corrects it from the real width, and everything below that width is a re-chunk, not an empty
    // page.
    private const int DEFAULT_COLUMN_COUNT = 8;

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
    private List<IconInfo[]> iconRows = [];
    private int columnCount = DEFAULT_COLUMN_COUNT;
    private IconInfo? selectedIcon;
    private bool isIconPanelOpen;
    private Dictionary<string, string>? iconGlyphs;
    private string? copyFeedbackKey;
    private DotNetObjectReference<IconographyPage>? dotnetObj;



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



    /// <summary>
    /// The install snippets are constant, so one Prism pass on the first render is all they need.
    /// The panel's own snippets are highlighted when it opens (see <see cref="OpenIconPanel"/>).
    /// The grid starts being watched here too - it only exists in the DOM once it has rendered.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _js.InvokeVoid("highlightSnippet");

            dotnetObj = DotNetObjectReference.Create(this);
            await _js.ObserveElementWidth(GRID_ELEMENT_ID, dotnetObj, nameof(OnIconGridResized));
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    /// <summary>
    /// The grid changed width, so the number of icons that fit across it may have changed. Rebuilding
    /// the rows is only worth a render when it actually did - a scrollbar appearing, or any resize
    /// smaller than a whole column, leaves the layout exactly as it was.
    /// </summary>
    [JSInvokable]
    public Task OnIconGridResized(double width)
    {
        if (width <= 0) return Task.CompletedTask;

        var columns = Math.Max(1, (int)((width + ICON_CELL_GAP) / (ICON_CELL_WIDTH + ICON_CELL_GAP)));

        if (columns == columnCount) return Task.CompletedTask;

        columnCount = columns;
        BuildIconRows();

        return InvokeAsync(StateHasChanged);
    }



    private void HandleClear()
    {
        filteredIcons = allIcons;
        BuildIconRows();
    }

    private void HandleChange(string text)
    {
        filteredIcons = string.IsNullOrEmpty(text)
            ? allIcons
            : allIcons.FindAll(icon =>
                icon.Value.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                icon.FieldName.Contains(text, StringComparison.InvariantCultureIgnoreCase));

        BuildIconRows();
    }

    /// <summary>
    /// Chunks the icons currently on show into the fixed-width rows the virtualizer renders one at a
    /// time. The count is <see cref="DEFAULT_COLUMN_COUNT"/> until the grid has been measured, so
    /// there are always rows to chunk into and an empty list means exactly what it says: nothing
    /// matched the search.
    /// </summary>
    private void BuildIconRows()
    {
        var rows = new List<IconInfo[]>((filteredIcons.Count + columnCount - 1) / columnCount);

        for (var i = 0; i < filteredIcons.Count; i += columnCount)
        {
            rows.Add([.. filteredIcons.GetRange(i, Math.Min(columnCount, filteredIcons.Count - i))]);
        }

        iconRows = rows;
    }

    private async Task OpenIconPanel(IconInfo icon)
    {
        selectedIcon = icon;
        isIconPanelOpen = true;
        copyFeedbackKey = null;
        await EnsureGlyphsLoadedAsync();

        // The panel's usage snippets are new markup every time a different icon is picked, so they
        // are highlighted here rather than once on the first render.
        StateHasChanged();
        await _js.InvokeVoid("highlightSnippet");
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



    public async ValueTask DisposeAsync()
    {
        try
        {
            await _js.UnobserveElementWidth(GRID_ELEMENT_ID);
        }
        catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unobserve

        dotnetObj?.Dispose();
        dotnetObj = null;

        GC.SuppressFinalize(this);
    }
}
