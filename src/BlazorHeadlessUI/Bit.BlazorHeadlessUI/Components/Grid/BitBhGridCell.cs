using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>Sort direction announced by a grid column header (<c>aria-sort</c>).</summary>
public enum BitBhSortDirection
{
    /// <summary>Not sorted (no <c>aria-sort</c>).</summary>
    None,

    /// <summary>Ascending.</summary>
    Ascending,

    /// <summary>Descending.</summary>
    Descending,
}

/// <summary>
/// A cell of a <see cref="BitBhGridRow"/>: <c>gridcell</c>, <c>columnheader</c>
/// (<see cref="Header"/>) or <c>rowheader</c> (<see cref="RowHeader"/>). Cells are the
/// focus targets of the grid (one tab stop); emits <c>data-focused</c>.
/// </summary>
public class BitBhGridCell : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    private bool _focusInside;
    private bool _selfFocusPending;

    [CascadingParameter]
    private BitBhGridRow? ParentRow { get; set; }

    /// <summary>A column header (<c>role="columnheader"</c>).</summary>
    [Parameter]
    public bool Header { get; set; }

    /// <summary>A row header (<c>role="rowheader"</c>).</summary>
    [Parameter]
    public bool RowHeader { get; set; }

    /// <summary>Sort state for column headers (<c>aria-sort</c>).</summary>
    [Parameter]
    public BitBhSortDirection Sort { get; set; }

    /// <summary>The 1-based column index for <c>aria-colindex</c> (when columns are hidden / virtualized).</summary>
    [Parameter]
    public int? ColIndex { get; set; }

    /// <summary>Invoked when the cell is activated with Enter or a click (e.g. sort on a header).</summary>
    [Parameter]
    public EventCallback OnActivate { get; set; }

    /// <summary>
    /// Makes this cell the grid's tab stop until the user moves focus (e.g. today's date
    /// in a calendar grid). Default: the first cell of the first body row.
    /// </summary>
    [Parameter]
    public bool InitialTabStop { get; set; }

    /// <summary>The cell content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal BitBhGridRow? Row => ParentRow;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => false;

    /// <inheritdoc />
    public string? ItemText => null;

    internal void Refresh() => StateHasChanged();

    protected override void OnInitialized() => ParentRow?.RegisterCell(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && ParentRow is not null)
            await ParentRow.CellCollection.EnsureOrderAsync(Interop);
    }

    public void Dispose() => ParentRow?.UnregisterCell(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddAttribute(31, "onfocus", On<FocusEventArgs>("onfocus", HandleFocus));
        builder.AddAttribute(32, "onclick", On<MouseEventArgs>("onclick", () => OnActivate.InvokeAsync()));
        builder.AddAttribute(33, "onfocusin", On<FocusEventArgs>("onfocusin", HandleFocusIn));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = Header ? "columnheader" : RowHeader ? "rowheader" : "gridcell";
        if (ColIndex is { } col) attrs["aria-colindex"] = col.ToString(CultureInfo.InvariantCulture);
        if (Header && Sort != BitBhSortDirection.None)
            attrs["aria-sort"] = Sort == BitBhSortDirection.Ascending ? "ascending" : "descending";
        var grid = ParentRow?.Grid;
        if (grid is not null) attrs["data-bit-bh-item"] = ParentRow!.CellCollection.Key;
        var focused = grid?.IsTabStop(this) ?? false;
        attrs["tabindex"] = focused ? "0" : "-1";
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowUp ArrowDown ArrowLeft ArrowRight Home End PageUp PageDown Space Ctrl+Home Ctrl+End Ctrl+a Meta+a";
        SetDataFlag(attrs, "focused", grid is not null && ReferenceEquals(grid.FocusedCell, this));
        return attrs;
    }

    // Keys pressed in interactive content of the cell (an input, a button) bubble to it too, but they belong to
    // that content. KeyboardEventArgs has no target, so the cell follows where focus is: its own focus event,
    // or a focusin bubbling up from something inside it (browsers send focus, then focusin).
    private void HandleFocus()
    {
        _selfFocusPending = true;
        _focusInside = false;
        ParentRow?.Grid?.SetFocus(this);
    }

    private void HandleFocusIn()
    {
        _focusInside = !_selfFocusPending;
        _selfFocusPending = false;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (_focusInside || ParentRow?.Grid is not { } grid) return;
        if (e.Key == "Enter" && OnActivate.HasDelegate)
        {
            await OnActivate.InvokeAsync();
            return;
        }
        await grid.Grid.HandleCellKeyDownAsync(this, e);
    }
}
