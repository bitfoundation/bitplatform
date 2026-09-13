using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Iconography;

public partial class IconographyPage
{
    /// <summary>
    /// How large a cell of the grid is drawn, in px. Three steps rather than a slider: the reader is
    /// choosing between scanning shapes and reading names, and there are only really two answers to
    /// that, plus one for a projector.
    /// <para>
    /// The height is not a free choice - it is the padding, the glyph, and the two lines the name is
    /// allowed - which is why it is derived here rather than typed. The stylesheet reads all of it
    /// back out of the custom properties the page sets on the scroller, so the geometry the
    /// virtualizer is told about and the geometry the browser lays out cannot drift apart.
    /// </para>
    /// </summary>
    private sealed record IconCell(string Label, int Width, int Glyph)
    {
        /// <summary>Padding, the glyph, the gap under it, and two lines of name.</summary>
        public int Height => 16 + Glyph + 6 + 26;

        /// <summary>What the virtualizer places rows at: the cell plus the gap that follows it.</summary>
        public int RowHeight => Height + GAP;
    }


    private const int GAP = 8;

    private const int RELATED_COUNT = 12;

    private const double PANEL_SIZE = 460;

    private const string GRID_ELEMENT_ID = "iconography-grid";

    private const string PANEL_ELEMENT_ID = "iconography-panel";

    private const string COPY_KEY_NAME = "name";
    private const string COPY_KEY_LINK = "link";

    // How wide the grid is assumed to be until it has been measured. Nothing measures anything while
    // the page is being prerendered - OnAfterRenderAsync never runs there - so a count of zero would
    // chunk the icons into no rows at all and ship a page with not one icon on it, to a crawler and
    // to a reader whose JS has not arrived yet. A desktop-ish guess instead: the first client frame
    // corrects it from the real width, and everything below that width is a re-chunk, not an empty
    // page.
    private const int DEFAULT_COLUMN_COUNT = 8;

    private static readonly IconCell[] cells =
    [
        new("Small", 72, 18),
        new("Medium", 92, 24),
        new("Large", 120, 34),
    ];

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

    private static readonly (string Label, BitVariant Value)[] previewVariants =
    [
        ("Text", BitVariant.Text),
        ("Outline", BitVariant.Outline),
        ("Fill", BitVariant.Fill),
    ];


    private string? _query;
    private string? _category;
    private int _cellIndex = 1;
    private bool _showNames;
    private bool _isBuilt;

    /// <summary>What the term and the category between them have left: the icons on show.</summary>
    private IReadOnlyList<IconEntry> _results = IconCatalog.Items;

    /// <summary>Those icons chunked into the fixed-width rows the virtualizer renders one at a time.</summary>
    private List<IconEntry[]> _rows = [];

    /// <summary>How many of the term's matches are in each category: the numbers on the chips.</summary>
    private int[] _counts = IconCatalog.CountByCategory(IconCatalog.Items);

    /// <summary>What the term finds before the category narrows it: the number on the "All" chip.</summary>
    private int _totalCount = IconCatalog.Items.Count;

    private int _columnCount = DEFAULT_COLUMN_COUNT;
    private bool _isGridMeasured;

    private IconEntry? _selected;
    private bool _isPanelOpen;
    private IReadOnlyList<IconEntry> _related = [];
    private Dictionary<string, string>? _glyphs;
    private string? _copyFeedbackKey;

    private BitVirtualize<IconEntry[]>? _grid;
    private IconEntry? _pendingScrollTo;
    private DotNetObjectReference<IconographyPage>? _dotnetObj;

    // The query values this page itself last wrote. A parameter that still matches one of them is
    // this page's own navigation coming back around rather than the reader arriving with something
    // different, so it must not overwrite what they have since typed or clicked.
    private string? _syncedQuery;
    private string? _syncedCategory;
    private string? _syncedIcon;


    private IconCell Cell => cells[_cellIndex];

    private bool IsFiltered => _query.HasValue() || _category is not null;


    /// <summary>The term to open filtered by, so that a search can be linked and reloaded.</summary>
    [SupplyParameterFromQuery(Name = "q")] public string? Query { get; set; }

    /// <summary>The category to open narrowed to.</summary>
    [SupplyParameterFromQuery(Name = "category")] public string? Category { get; set; }

    /// <summary>
    /// The icon to open the details panel on. It is what makes an icon a thing that can be sent to
    /// someone - "use this one" as a link, rather than as a name they then have to find again.
    /// </summary>
    [SupplyParameterFromQuery(Name = "icon")] public string? Icon { get; set; }


    protected override Task OnParamsSetAsync()
    {
        // Only adopt a query value that is not the one this page put there. Anything else is the
        // round trip of its own NavigateTo, and taking it again would undo a keystroke made between
        // the navigation and the re-render.
        var rebuild = _isBuilt is false;

        if (Differs(Query, _syncedQuery))
        {
            _syncedQuery = Query;
            _query = Query;
            rebuild = true;
        }

        if (Differs(Category, _syncedCategory))
        {
            _syncedCategory = Category;
            _category = IconCatalog.ResolveCategory(Category);
            rebuild = true;
        }

        if (Differs(Icon, _syncedIcon))
        {
            _syncedIcon = Icon;

            if (IconCatalog.Find(Icon) is { } icon)
            {
                Select(icon);

                // Which row it sits in cannot be worked out until the grid has been measured, so the
                // scroll is a request rather than a call (see OnAfterRenderAsync).
                _pendingScrollTo = icon;
            }
            else
            {
                _isPanelOpen = false;
                _selected = null;
            }
        }

        if (rebuild)
        {
            _isBuilt = true;
            Rebuild();
        }

        return base.OnParamsSetAsync();
    }

    /// <summary>
    /// The install snippets are constant, so one Prism pass on the first render is all they need;
    /// the panel's own snippets are highlighted when it opens (see <see cref="OpenPanel"/>). The
    /// grid starts being watched here too - it only exists in the DOM once it has rendered.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoid("highlightSnippet");

            _dotnetObj = DotNetObjectReference.Create(this);
            await JSRuntime.ObserveElementWidth(GRID_ELEMENT_ID, _dotnetObj, nameof(OnIconGridResized));

            // A link that arrives with an icon already open needs the glyph table the panel's
            // Unicode row is read from, which normally only loads when an icon is clicked.
            if (_selected is not null)
            {
                await EnsureGlyphsLoadedAsync();
                StateHasChanged();
            }
        }

        // A deep-linked icon is worth nothing if the reader then has to find it in a list of two
        // thousand, so the grid is scrolled to its row - once the measurement that says which row
        // that is has come back.
        if (_pendingScrollTo is not null && _isGridMeasured && _grid is not null)
        {
            var icon = _pendingScrollTo;
            _pendingScrollTo = null;

            var index = _rows.FindIndex(row => Array.IndexOf(row, icon) >= 0);

            if (index >= 0)
            {
                await _grid.ScrollToIndexAsync(index, BitVirtualizeScrollAlignment.Center);
            }
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

        var wasMeasured = _isGridMeasured;
        _isGridMeasured = true;

        var columns = Math.Max(1, (int)((width + GAP) / (Cell.Width + GAP)));

        if (columns == _columnCount)
        {
            // Nothing to re-chunk - but the first measurement is also what releases a pending
            // scroll, and that needs a render to reach OnAfterRenderAsync.
            return wasMeasured ? Task.CompletedTask : InvokeAsync(StateHasChanged);
        }

        _columnCount = columns;
        _rows = BuildRows();

        return InvokeAsync(StateHasChanged);
    }


    private void ApplyQuery(string? value)
    {
        _query = value;
        Rebuild();
        SyncUrl();
    }

    private void SelectCategory(string? category)
    {
        // Clicking the category you are already in clears it, which is what a pressed toggle implies.
        _category = _category == category ? null : category;
        Rebuild();
        SyncUrl();
    }

    private void ClearFilters()
    {
        _query = null;
        _category = null;
        Rebuild();
        SyncUrl();
    }

    /// <summary>
    /// A bigger cell is a smaller column count, and the width it is derived from has not changed -
    /// so the rows are re-chunked here rather than waiting for a resize that will never come.
    /// </summary>
    private void SelectCell(int index)
    {
        // Deliberately not in the URL: it is how this reader likes to read, not what they are
        // looking at, and a shared link should open at the size its recipient chose.
        _cellIndex = index;
        _rows = BuildRows();
    }

    private void ToggleNames()
    {
        _showNames = _showNames is false;
    }

    /// <summary>
    /// Rebuilds what is on show from the current term and category. It runs the whole set through a
    /// ranking pass, which at two thousand short strings is cheap enough to do on every keystroke -
    /// and the search box debounces anyway.
    /// </summary>
    private void Rebuild()
    {
        var matches = IconCatalog.Search(_query);

        _totalCount = matches.Count;

        // Blind to the selected category on purpose, so a chip keeps saying what the term finds in
        // it while a different one is active - which is the only way the row can be used to move
        // between categories rather than only into one.
        _counts = IconCatalog.CountByCategory(matches);

        _results = IconCatalog.InCategory(matches, _category);
        _rows = BuildRows();
    }

    /// <summary>
    /// Chunks the icons on show into the fixed-width rows the virtualizer renders one at a time. The
    /// column count is <see cref="DEFAULT_COLUMN_COUNT"/> until the grid has been measured, so there
    /// are always rows to chunk into and an empty list means exactly what it says: nothing matched.
    /// </summary>
    private List<IconEntry[]> BuildRows()
    {
        var rows = new List<IconEntry[]>((_results.Count + _columnCount - 1) / _columnCount);

        for (var i = 0; i < _results.Count; i += _columnCount)
        {
            var count = Math.Min(_columnCount, _results.Count - i);
            var row = new IconEntry[count];

            for (var c = 0; c < count; c++)
            {
                row[c] = _results[i + c];
            }

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Writes the term, the category and the open icon into the address bar, so a filtered grid and
    /// an opened icon are a page that can be linked, bookmarked and reloaded rather than a state
    /// that exists only in this tab. Replaces rather than pushes: a history stack with one entry per
    /// keystroke turns the back button into a way of deleting characters one at a time.
    /// </summary>
    private void SyncUrl()
    {
        _syncedQuery = _query.HasValue() ? _query : null;
        _syncedCategory = _category;
        _syncedIcon = _isPanelOpen ? _selected?.Name : null;

        var query = new List<string>();

        if (_syncedQuery is not null) query.Add($"q={Uri.EscapeDataString(_syncedQuery)}");
        if (_syncedCategory is not null) query.Add($"category={Uri.EscapeDataString(_syncedCategory)}");
        if (_syncedIcon is not null) query.Add($"icon={Uri.EscapeDataString(_syncedIcon)}");

        var url = query.Count > 0 ? $"/iconography?{string.Join('&', query)}" : "/iconography";

        NavigationManager.NavigateTo(url, replace: true);
    }

    private string IconLink(IconEntry icon)
    {
        return NavigationManager.ToAbsoluteUri($"/iconography?icon={Uri.EscapeDataString(icon.Name)}").ToString();
    }


    private void Select(IconEntry icon)
    {
        _selected = icon;
        _isPanelOpen = true;
        _copyFeedbackKey = null;
        _related = IconCatalog.Related(icon, RELATED_COUNT);
    }

    private async Task OpenPanel(IconEntry icon)
    {
        Select(icon);
        SyncUrl();

        await EnsureGlyphsLoadedAsync();

        // The panel's usage snippets are new markup every time a different icon is picked, so they
        // are highlighted here rather than once on the first render - and only the panel is passed
        // over, because the page's own snippets were done on the first render and re-running Prism
        // across all of them on every click is work that changes nothing.
        StateHasChanged();
        await JSRuntime.InvokeVoid("highlightSnippet", PANEL_ELEMENT_ID);
    }

    private async Task ClosePanel()
    {
        _isPanelOpen = false;
        SyncUrl();
        StateHasChanged();

        await Task.Delay(200);

        if (_isPanelOpen is false)
        {
            _selected = null;
            _related = [];
            StateHasChanged();
        }
    }

    private Task HandlePanelDismissed(MouseEventArgs _) => ClosePanel();


    /// <summary>
    /// The code point behind a glyph, read out of the icon stylesheet itself rather than kept in a
    /// second table here. It is what an app needs in order to draw the icon from CSS of its own.
    /// </summary>
    private string? GetGlyphCode(IconEntry icon)
    {
        if (_glyphs is null) return null;

        if (_glyphs.TryGetValue(icon.Name, out var glyph) is false || string.IsNullOrEmpty(glyph)) return null;

        if (glyph[0] == '\\' && int.TryParse(glyph[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var escaped))
        {
            return $"\\{escaped:X4}";
        }

        return $"\\{char.ConvertToUtf32(glyph, 0):X4}";
    }

    private async Task EnsureGlyphsLoadedAsync()
    {
        if (_glyphs is not null) return;

        try
        {
            var css = await HttpClient.GetStringAsync("_content/Bit.BlazorUI.Icons/styles/bit.blazorui.icons.css");
            _glyphs = Regex.Matches(css, @"\.bit-icon--([^:{]+)::before\{content:""([^""]+)""\}")
                           .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        }
        catch
        {
            _glyphs = null;
        }
    }


    private Task CopyName() => Copy(_selected!.Name, COPY_KEY_NAME);

    private Task CopyLink() => Copy(IconLink(_selected!), COPY_KEY_LINK);

    private Task HandleDetailCopy((string Text, string Key) args) => Copy(args.Text, args.Key);

    private async Task Copy(string text, string feedbackKey)
    {
        await JSRuntime.CopyToClipboard(text);
        _copyFeedbackKey = feedbackKey;
        StateHasChanged();

        await Task.Delay(1500);

        if (_copyFeedbackKey == feedbackKey)
        {
            _copyFeedbackKey = null;
            StateHasChanged();
        }
    }


    private static bool Differs(string? left, string? right)
    {
        return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal) is false;
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        try
        {
            await JSRuntime.UnobserveElementWidth(GRID_ELEMENT_ID);
        }
        catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unobserve

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await base.DisposeAsync(disposing);
    }
}
