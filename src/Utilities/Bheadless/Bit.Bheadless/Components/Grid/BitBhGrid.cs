using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>Row selection behaviour of a <see cref="BitBhGrid"/>.</summary>
public enum BitBhGridSelectionMode
{
    /// <summary>Rows can't be selected.</summary>
    None,

    /// <summary>One selected row.</summary>
    Single,

    /// <summary>Many selected rows (Space toggles, Shift+Space / Shift+click extends, Ctrl+A selects all).</summary>
    Multiple,
}

/// <summary>
/// A headless, accessible interactive Grid / TreeGrid implementing the WAI-ARIA
/// Data Grid and Treegrid patterns: a two-dimensional, single-tab-stop widget
/// whose cells are navigated with the arrow keys. Bring your own table / div
/// markup and styling; the grid supplies roles, roving focus, selection and
/// expansion.
///
/// <para><b>Keyboard:</b> arrows move between cells, Home / End to the row's ends,
/// Ctrl+Home / Ctrl+End to the grid's corners, PageUp / PageDown by
/// <see cref="PageSize"/> rows, Space selects the row, Shift+Space / Ctrl+A extend,
/// Enter activates. In a treegrid, ArrowRight on a row's first cell expands it
/// (or moves into it), ArrowLeft collapses it (or moves to the parent row).</para>
///
/// <code>
/// &lt;BitBhGrid aria-label="Files" As="table" SelectionMode="BitBhGridSelectionMode.Multiple" @bind-SelectedKeys="selected"&gt;
///     &lt;BitBhGridRowGroup As="thead"&gt;
///         &lt;BitBhGridRow As="tr"&gt;&lt;BitBhGridCell As="th" Header="true"&gt;Name&lt;/BitBhGridCell&gt;…&lt;/BitBhGridRow&gt;
///     &lt;/BitBhGridRowGroup&gt;
///     &lt;BitBhGridRowGroup As="tbody"&gt;
///         @foreach (var f in files)
///         {
///             &lt;BitBhGridRow As="tr" Key="@f.Id"&gt;&lt;BitBhGridCell As="td"&gt;@f.Name&lt;/BitBhGridCell&gt;…&lt;/BitBhGridRow&gt;
///         }
///     &lt;/BitBhGridRowGroup&gt;
/// &lt;/BitBhGrid&gt;
/// </code>
/// </summary>
public class BitBhGrid : BitBhComponentBase
{
    private BitBhGridContext? _context;
    private HashSet<string> _selected = new(StringComparer.Ordinal);
    private HashSet<string> _expanded = new(StringComparer.Ordinal);
    private BitBhGridRow? _anchor;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Render <c>role="treegrid"</c> with expandable rows.</summary>
    [Parameter]
    public bool TreeGrid { get; set; }

    /// <summary>Row selection. Default: none.</summary>
    [Parameter]
    public BitBhGridSelectionMode SelectionMode { get; set; }

    /// <summary>Selected row keys. Supplying it makes selection controlled (use <c>@bind-SelectedKeys</c>).</summary>
    [Parameter]
    public IEnumerable<string>? SelectedKeys { get; set; }

    /// <summary>Two-way binding callback for <see cref="SelectedKeys"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> SelectedKeysChanged { get; set; }

    /// <summary>Expanded row keys (treegrid). Supplying it makes expansion controlled (use <c>@bind-ExpandedKeys</c>).</summary>
    [Parameter]
    public IEnumerable<string>? ExpandedKeys { get; set; }

    /// <summary>Two-way binding callback for <see cref="ExpandedKeys"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ExpandedKeysChanged { get; set; }

    /// <summary>Initially expanded rows when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<string>? DefaultExpandedKeys { get; set; }

    /// <summary>Invoked on Enter or double click on a row.</summary>
    [Parameter]
    public EventCallback<string> OnRowActivate { get; set; }

    /// <summary>Rows moved by PageUp / PageDown. Default: 10.</summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>Reading direction. Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Rows, row groups and cells.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal BitBhGridContext Context => _context ??= new BitBhGridContext(this, BitBhId.New("bit-bh-grid"));
    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    protected override void OnInitialized()
    {
        if (DefaultExpandedKeys is not null) _expanded = new HashSet<string>(DefaultExpandedKeys, StringComparer.Ordinal);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await Context.Rows.EnsureOrderAsync(Interop);
        // Cells render before all rows registered: settle the single tab stop once.
        if (firstRender && Context.FocusedCell is null) Context.RefreshCells();
    }

    /// <summary>Expands or collapses the row with <paramref name="key"/> (treegrid).</summary>
    public Task SetExpandedAsync(string key, bool expanded)
        => Context.Rows.Items.FirstOrDefault(r => r.Key == key) is { } row ? SetExpandedAsync(row, expanded) : Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhGridContext>>(40);
        builder.AddComponentParameter(41, "Value", Context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = TreeGrid ? "treegrid" : "grid";
        if (SelectionMode == BitBhGridSelectionMode.Multiple) attrs["aria-multiselectable"] = "true";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        return attrs;
    }

    // ── State ────────────────────────────────────────────────────────────────

    internal bool IsSelected(string? key)
        => key is not null && (IsProvided(nameof(SelectedKeys)) ? SelectedKeys?.Contains(key) ?? false : _selected.Contains(key));

    internal bool IsExpanded(string? key)
        => key is not null && (IsProvided(nameof(ExpandedKeys)) ? ExpandedKeys?.Contains(key) ?? false : _expanded.Contains(key));

    internal async Task SetExpandedAsync(BitBhGridRow row, bool expanded)
    {
        if (row.Key is null || !row.HasChildren || IsExpanded(row.Key) == expanded) return;
        var set = new HashSet<string>(IsProvided(nameof(ExpandedKeys)) ? ExpandedKeys ?? [] : _expanded, StringComparer.Ordinal);
        if (expanded) set.Add(row.Key); else set.Remove(row.Key);
        if (!IsProvided(nameof(ExpandedKeys))) _expanded = set;
        // Collapsing hides descendants: keep the tab stop on a visible row. Asked of the hierarchy, not of
        // IsRowVisible: a controlled ExpandedKeys still lists the row until the parent re-renders.
        if (!expanded && Context.FocusedCell?.Row is { } focusedRow && Context.IsDescendantOf(focusedRow, row.Key) && row.Cells.Count > 0)
            Context.SetFocus(row.Cells[0]);
        await ExpandedKeysChanged.InvokeAsync(set.ToArray());
        StateHasChanged();
    }

    internal async Task SelectRowAsync(BitBhGridRow row, bool toggle, bool range)
    {
        if (SelectionMode == BitBhGridSelectionMode.None || row.Key is null || row.Disabled) return;
        var set = new HashSet<string>(IsProvided(nameof(SelectedKeys)) ? SelectedKeys ?? [] : _selected, StringComparer.Ordinal);

        if (SelectionMode == BitBhGridSelectionMode.Single)
        {
            set = toggle && set.Contains(row.Key) ? new(StringComparer.Ordinal) : new(StringComparer.Ordinal) { row.Key };
        }
        else if (range && _anchor is not null)
        {
            var rows = Context.VisibleRows().Where(r => !r.IsHeaderRow).ToList();
            var a = rows.IndexOf(_anchor);
            var b = rows.IndexOf(row);
            if (a >= 0 && b >= 0)
                for (var i = Math.Min(a, b); i <= Math.Max(a, b); i++)
                    if (rows[i].Key is { } k && !rows[i].Disabled) set.Add(k);
        }
        else
        {
            if (!set.Remove(row.Key)) set.Add(row.Key);
            _anchor = row;
        }

        if (!IsProvided(nameof(SelectedKeys))) _selected = set;
        await SelectedKeysChanged.InvokeAsync(set.ToArray());
        StateHasChanged();
    }

    private async Task SelectAllAsync()
    {
        if (SelectionMode != BitBhGridSelectionMode.Multiple) return;
        var set = new HashSet<string>(Context.VisibleRows().Where(r => !r.IsHeaderRow && !r.Disabled && r.Key is not null).Select(r => r.Key!), StringComparer.Ordinal);
        if (!IsProvided(nameof(SelectedKeys))) _selected = set;
        await SelectedKeysChanged.InvokeAsync(set.ToArray());
        StateHasChanged();
    }

    // ── Keyboard ─────────────────────────────────────────────────────────────

    internal async Task HandleCellKeyDownAsync(BitBhGridCell cell, KeyboardEventArgs e)
    {
        var row = cell.Row;
        if (row is null) return;
        await Context.Rows.EnsureOrderAsync(Interop);
        await row.CellCollection.EnsureOrderAsync(Interop);

        var rows = Context.VisibleRows();
        var rowIndex = rows.IndexOf(row);
        var colIndex = row.IndexOfCell(cell);
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var right = rtl ? "ArrowLeft" : "ArrowRight";
        var left = rtl ? "ArrowRight" : "ArrowLeft";
        var ctrl = e.CtrlKey || e.MetaKey;

        if (e.Key == right)
        {
            if (TreeGrid && colIndex == 0 && row.HasChildren && !IsExpanded(row.Key)) await SetExpandedAsync(row, true);
            else await FocusCellAsync(row, colIndex + 1);
        }
        else if (e.Key == left)
        {
            if (TreeGrid && colIndex == 0 && row.HasChildren && IsExpanded(row.Key)) await SetExpandedAsync(row, false);
            else if (TreeGrid && colIndex == 0 && row.ParentKey is { } parentKey) await FocusCellAsync(rows.FirstOrDefault(r => r.Key == parentKey), 0);
            else await FocusCellAsync(row, colIndex - 1);
        }
        else switch (e.Key)
        {
            case "ArrowDown":
                await FocusCellAsync(rows.ElementAtOrDefault(rowIndex + 1), colIndex);
                if (e.ShiftKey && rows.ElementAtOrDefault(rowIndex + 1) is { } down) await SelectRowAsync(down, false, true);
                break;
            case "ArrowUp":
                await FocusCellAsync(rows.ElementAtOrDefault(rowIndex - 1), colIndex);
                if (e.ShiftKey && rows.ElementAtOrDefault(rowIndex - 1) is { } up) await SelectRowAsync(up, false, true);
                break;
            case "PageDown":
                await FocusCellAsync(rows.ElementAtOrDefault(Math.Min(rows.Count - 1, rowIndex + PageSize)), colIndex);
                break;
            case "PageUp":
                await FocusCellAsync(rows.ElementAtOrDefault(Math.Max(0, rowIndex - PageSize)), colIndex);
                break;
            case "Home":
                await FocusCellAsync(ctrl ? rows.FirstOrDefault() : row, 0);
                break;
            case "End":
                var target = ctrl ? rows.LastOrDefault() : row;
                await FocusCellAsync(target, (target?.Cells.Count ?? 1) - 1);
                break;
            case " ":
                // A held key repeats: the selection would flip back and forth.
                if (e.Repeat) break;
                await SelectRowAsync(row, toggle: true, range: e.ShiftKey);
                break;
            case "a" or "A" when ctrl:
                await SelectAllAsync();
                break;
            case "Enter":
                if (row.Key is not null) await OnRowActivate.InvokeAsync(row.Key);
                break;
        }
    }

    internal async Task FocusCellAsync(BitBhGridRow? row, int col)
    {
        if (row is null) return;
        await row.CellCollection.EnsureOrderAsync(Interop);
        if (row.Cells.Count == 0) return;
        var cell = row.Cells[Math.Clamp(col, 0, row.Cells.Count - 1)];
        Context.SetFocus(cell);
        await Interop.FocusAsync(cell.ItemId);
    }

    internal async Task HandleRowClickAsync(BitBhGridRow row, MouseEventArgs e)
    {
        if (SelectionMode == BitBhGridSelectionMode.None || row.IsHeaderRow) return;
        if (SelectionMode == BitBhGridSelectionMode.Multiple)
            await SelectRowAsync(row, toggle: true, range: e.ShiftKey);
        else
            await SelectRowAsync(row, toggle: false, range: false);
        if (e.Detail == 2 && row.Key is not null) await OnRowActivate.InvokeAsync(row.Key);
    }
}
