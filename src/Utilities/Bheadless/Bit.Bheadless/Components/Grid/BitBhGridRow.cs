using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A row of a <see cref="BitBhGrid"/> (<c>role="row"</c>). Give data rows a unique
/// <see cref="Key"/> for selection / expansion. In a treegrid set <see cref="Level"/>,
/// <see cref="HasChildren"/> and <see cref="ParentKey"/>: rows under a collapsed ancestor are
/// <c>hidden</c> and skipped by keyboard navigation (or render them only while the parent is
/// expanded — see <see cref="BitBhGridContext.IsExpanded"/>).
/// Emits <c>data-selected</c>, <c>data-state="open|closed"</c>, <c>data-level</c>.
/// </summary>
public class BitBhGridRow : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    // Cells in document order: registration order breaks as soon as a column is shown after the others.
    private readonly BitBhCollection<BitBhGridCell> _cells = new(BitBhId.New("bit-bh-cells"));

    [CascadingParameter]
    private BitBhGridContext? GridContext { get; set; }

    /// <summary>The row's key (required for selection and expansion).</summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>Hierarchy level (treegrid), starting at 1.</summary>
    [Parameter]
    public int? Level { get; set; }

    /// <summary>Whether the row has child rows (treegrid).</summary>
    [Parameter]
    public bool HasChildren { get; set; }

    /// <summary>The parent row's key (treegrid): hides the row while the parent is collapsed and enables ArrowLeft to the parent.</summary>
    [Parameter]
    public string? ParentKey { get; set; }

    /// <summary>Marks a header row (excluded from selection).</summary>
    [Parameter]
    public bool IsHeaderRow { get; set; }

    /// <summary>Disables selection of the row.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The 1-based row index for <c>aria-rowindex</c> (virtualized grids).</summary>
    [Parameter]
    public int? RowIndex { get; set; }

    /// <summary>The row's cells.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal IReadOnlyList<BitBhGridCell> Cells => _cells.Items;

    internal BitBhCollection<BitBhGridCell> CellCollection => _cells;

    internal bool IsRegistered { get; private set; }

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled;

    /// <inheritdoc />
    public string? ItemText => null;

    internal BitBhGridContext? Grid => GridContext;

    internal void RegisterCell(BitBhGridCell cell) => _cells.Register(cell);

    internal void UnregisterCell(BitBhGridCell cell) => _cells.Unregister(cell);

    internal int IndexOfCell(BitBhGridCell cell) => _cells.IndexOf(cell);

    protected override void OnInitialized()
    {
        GridContext?.Rows.Register(this);
        IsRegistered = GridContext is not null;
        // The default tab stop is the first cell: when the browser re-sorts them it can move.
        _cells.Changed += () =>
        {
            if (GridContext is { FocusedCell: null })
                foreach (var cell in _cells.Items) cell.Refresh();
        };
    }

    public void Dispose()
    {
        IsRegistered = false;
        GridContext?.Rows.Unregister(this);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhGridRow>>(50);
        builder.AddComponentParameter(51, "Value", this);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", (RenderFragment)(inner =>
        {
            // The state changes every render: parts such as <BitBhGridRowToggle /> re-render with it.
            inner.OpenComponent<CascadingValue<BitBhGridRowState>>(0);
            inner.AddComponentParameter(1, "Value", new BitBhGridRowState(
                Key, Level, HasChildren,
                GridContext?.IsExpanded(Key) ?? false,
                GridContext?.IsSelected(Key) ?? false));
            inner.AddComponentParameter(2, "ChildContent", ChildContent);
            inner.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "row";
        if (RowIndex is { } index) attrs["aria-rowindex"] = index.ToString(CultureInfo.InvariantCulture);
        if (GridContext is not null)
        {
            attrs["data-bit-bh-item"] = GridContext.Rows.Key;
            if (GridContext.IsTreeGrid)
            {
                if (Level is { } level)
                {
                    attrs["aria-level"] = level.ToString(CultureInfo.InvariantCulture);
                    attrs["data-level"] = level.ToString(CultureInfo.InvariantCulture);
                    attrs["style"] = FormattableString.Invariant($"--bit-bh-grid-level:{level}");
                }
                if (HasChildren)
                {
                    var expanded = GridContext.IsExpanded(Key);
                    SetAriaBool(attrs, "aria-expanded", expanded);
                    SetDataState(attrs, expanded);
                }
            }
            if (GridContext.SelectionMode != BitBhGridSelectionMode.None && !IsHeaderRow)
            {
                var selected = GridContext.IsSelected(Key);
                SetAriaBool(attrs, "aria-selected", selected);
                SetDataFlag(attrs, "selected", selected);
            }
        }
        if (GridContext is not null && !GridContext.IsRowVisible(this)) attrs["hidden"] = true;
        if (Disabled) attrs["aria-disabled"] = "true";
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs e) => GridContext?.Grid.HandleRowClickAsync(this, e) ?? Task.CompletedTask;
}
