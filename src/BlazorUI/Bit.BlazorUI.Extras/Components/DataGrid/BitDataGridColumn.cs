using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

/// <summary>
/// Defines a column inside a <see cref="BitDataGrid{TItem}"/>. Place these as child
/// content of the grid. A column can be bound to a property via <see cref="Field"/>
/// or be a purely template-driven column.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public class BitDataGridColumn<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] internal BitDataGrid<TItem>? Grid { get; set; }

    /// <summary>Name of the property this column is bound to. Supports nested paths ("Address.City").
    /// Prefer <see cref="Property"/> for a strongly typed, refactor-safe alternative.</summary>
    [Parameter] public string? Field { get; set; }

    /// <summary>
    /// Typed selector of the property this column is bound to, e.g. <c>Property="p => p.Name"</c>.
    /// A strongly typed, refactor-safe alternative to <see cref="Field"/> that supports nested member
    /// chains (<c>p => p.Address.City</c>). Takes precedence over <see cref="Field"/> when both are set.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object?>>? Property { get; set; }

    /// <summary>Stable identifier for the column. Defaults to <see cref="Field"/>.</summary>
    [Parameter] public string? ColumnId { get; set; }

    /// <summary>Header text. Defaults to a humanized <see cref="Field"/>.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>CSS width, e.g. "120px" or "20%". When null the column shares remaining space.</summary>
    [Parameter] public string? Width { get; set; }

    [Parameter] public int MinWidth { get; set; } = 60;

    /// <summary>Maximum width in pixels the column can be resized to. When null the column is unbounded.</summary>
    [Parameter] public int? MaxWidth { get; set; }

    [Parameter] public bool? Sortable { get; set; }

    /// <summary>
    /// Optional custom sort key selector. Enables sorting for template-only columns (no
    /// <see cref="Field"/>) and overrides the field value as the sort key when both are set —
    /// e.g. sort a display-name column by last name.
    /// </summary>
    [Parameter] public Func<TItem, object?>? SortBy { get; set; }

    /// <summary>
    /// When true, the first click on the header sorts descending instead of ascending.
    /// Mirrors react-data-grid's <c>sortDescendingFirst</c>.
    /// </summary>
    [Parameter] public bool SortDescendingFirst { get; set; }
    /// <summary>
    /// Optional validator for inline edits. Receives the row being edited and the proposed (already
    /// type-converted) value; returns an error message to reject it, or <c>null</c> to accept.
    /// While any column of the row in edit mode has an error, saving is blocked and the message is
    /// shown under the editor.
    /// </summary>
    [Parameter] public Func<TItem, object?, string?>? Validate { get; set; }

    [Parameter] public bool? Filterable { get; set; }

    /// <summary>Overrides the grid-level <c>FilterOperators</c> toggle (the operator dropdown next to
    /// this column's filter editor).</summary>
    [Parameter] public bool? FilterOperators { get; set; }
    [Parameter] public bool? Resizable { get; set; }
    [Parameter] public bool? Reorderable { get; set; }
    [Parameter] public bool? Editable { get; set; }
    [Parameter] public bool? Groupable { get; set; }

    /// <summary>Pin the column to the start edge so it stays visible while scrolling horizontally.</summary>
    [Parameter] public bool Frozen { get; set; }

    /// <summary>Pin the column to the end edge (right in LTR, left in RTL) so it stays visible while
    /// scrolling horizontally. Typical for action/status columns. Ignored when <see cref="Frozen"/> is set.</summary>
    [Parameter] public bool FrozenEnd { get; set; }

    /// <summary>
    /// Optional header group name. Consecutive columns sharing the same value are rendered
    /// under a single spanning header cell. Mirrors react-data-grid's column groups.
    /// </summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>
    /// Optional per-row column span. Returns how many columns the cell should occupy
    /// (>= 1), or null/1 for no spanning. Mirrors react-data-grid's <c>colSpan</c>.
    /// </summary>
    [Parameter] public Func<TItem, int?>? ColSpan { get; set; }

    [Parameter] public bool Visible { get; set; } = true;

    [Parameter] public BitDataGridColumnAlign Align { get; set; } = BitDataGridColumnAlign.Left;

    /// <summary>A .NET format string applied to the value (e.g. "C2", "yyyy-MM-dd").</summary>
    [Parameter] public string? Format { get; set; }

    [Parameter] public BitDataGridColumnDataType DataType { get; set; } = BitDataGridColumnDataType.Auto;

    [Parameter] public BitDataGridAggregateType Aggregate { get; set; } = BitDataGridAggregateType.None;

    /// <summary>
    /// Optional custom aggregate function, for computations beyond the built-in
    /// Sum/Average/Count/Min/Max — e.g. a distinct count or a weighted average. Receives the rows of
    /// the footer's view (or of each group) and returns the aggregate value; <see cref="AggregateFormat"/>
    /// (or <see cref="Format"/>) is applied to it for display. Takes precedence over <see cref="Aggregate"/>.
    /// </summary>
    [Parameter] public Func<IReadOnlyList<TItem>, object?>? AggregateBy { get; set; }

    /// <summary>Format string for the aggregate value. Falls back to <see cref="Format"/>.</summary>
    [Parameter] public string? AggregateFormat { get; set; }

    [Parameter] public string? HeaderClass { get; set; }
    [Parameter] public string? CellClass { get; set; }

    /// <summary>Custom rendering for a data cell.</summary>
    [Parameter] public RenderFragment<TItem>? Template { get; set; }

    /// <summary>Custom rendering for the header cell content.</summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Custom editor rendered when the row/cell is in edit mode.</summary>
    [Parameter] public RenderFragment<TItem>? EditTemplate { get; set; }

    /// <summary>Custom rendering for the footer/aggregate cell.</summary>
    [Parameter] public RenderFragment<BitDataGridAggregateResult>? FooterTemplate { get; set; }

    // ---- Runtime state (managed by the grid) ----

    /// <summary>Current resolved width applied via inline style (set by resizing).</summary>
    internal double? ResizedWidth { get; set; }

    /// <summary>The column's header cell element, captured so a starting resize can measure the real
    /// rendered width (needed when <see cref="Width"/> is expressed in non-px units like % or fr).</summary>
    internal ElementReference HeaderCellRef { get; set; }

    internal BitDataGridPropertyAccessor<TItem>? Accessor { get; private set; }

    // The property path extracted from the Property expression. Razor recreates the expression instance
    // every render, so the extracted string (not the expression reference) is what the rest of the column
    // compares and registers with — it stays stable across renders for the same selector.
    private string? _propertyPath;

    /// <summary>The resolved property path this column binds to: <see cref="Property"/>'s member chain
    /// when set, otherwise <see cref="Field"/>.</summary>
    internal string? EffectiveField => _propertyPath ?? Field;

    // Treat empty/whitespace ColumnId and the resolved field as "unset" (matching HasField's emptiness
    // check) so an accidental blank value can't become a column id. Blank ids would collide across columns
    // instead of each falling back to a unique generated id, so only use the generated fallback when both
    // are unset.
    internal string Id => !string.IsNullOrWhiteSpace(ColumnId) ? ColumnId
                        : !string.IsNullOrWhiteSpace(EffectiveField) ? EffectiveField
                        : $"col-{GetHashCode():x}";

    // The id this column is currently registered under in the grid. Tracked separately from Id so a
    // later change to ColumnId/Field can re-register the column under its new id (and the old entry
    // can be removed) rather than leaving a stale registry key behind.
    private string? _registeredId;

    // Snapshot of the parameters that affect the grid's computed view/aggregates. When any of these
    // change after registration the grid must recompute even if the resolved Id is unchanged (e.g. a
    // fixed ColumnId with a mutated Field, or a changed Aggregate/Format).
    private string? _lastField;
    private BitDataGridAggregateType _lastAggregate;
    private string? _lastFormat;
    private string? _lastAggregateFormat;

    // Tracked separately from the semantic snapshot: a Visible change only affects layout (the grid's
    // cached visible-column list), not the computed view, so it must not trigger a data refresh.
    private bool _lastVisible = true;

    // Frozen/FrozenEnd are layout-only too: sticky positioning and the cached column slots depend on
    // them, so a change must invalidate the grid's layout caches without refreshing data.
    private bool _lastFrozen;
    private bool _lastFrozenEnd;

    internal string DisplayTitle => Title ?? Humanize(EffectiveField) ?? Id;

    internal bool HasField => !string.IsNullOrWhiteSpace(EffectiveField);

    /// <summary>The effective sort key selector: <see cref="SortBy"/> when provided, otherwise the
    /// field accessor's value. Null when the column has neither (such a column cannot sort).</summary>
    internal Func<TItem, object?>? SortKey => SortBy ?? (Accessor is { } a ? a.GetValue : null);

    internal BitDataGridColumnDataType EffectiveDataType
    {
        get
        {
            if (DataType != BitDataGridColumnDataType.Auto) return DataType;
            if (Accessor is null) return BitDataGridColumnDataType.Text;
            var t = Accessor.UnderlyingType;
            if (t == typeof(bool)) return BitDataGridColumnDataType.Boolean;
            if (t.IsEnum) return BitDataGridColumnDataType.Enum;
            if (t == typeof(DateOnly)) return BitDataGridColumnDataType.Date;
            // DateTime carries a time component, so use a time-aware editor rather than the date-only
            // control DateOnly uses. DateTimeOffset additionally carries a UTC offset that a plain
            // datetime-local control cannot represent, so it gets its own offset-preserving path.
            if (t == typeof(DateTime)) return BitDataGridColumnDataType.DateTime;
            if (t == typeof(DateTimeOffset)) return BitDataGridColumnDataType.DateTimeOffset;
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
                || t == typeof(sbyte) || t == typeof(ushort) || t == typeof(uint) || t == typeof(ulong)
                || t == typeof(double) || t == typeof(float) || t == typeof(decimal))
                return BitDataGridColumnDataType.Number;
            return BitDataGridColumnDataType.Text;
        }
    }

    protected override void OnInitialized()
    {
        if (Grid is null)
            throw new InvalidOperationException($"{nameof(BitDataGridColumn<TItem>)} must be used inside a {nameof(BitDataGrid<TItem>)}.");

        // Resolve the accessor before registering with the grid. AddColumn recomputes footer/aggregate
        // values immediately, so a field-bound aggregate column whose Accessor was still null at that
        // point would be skipped on first registration.
        _propertyPath = Property is null ? null : BitDataGridPropertyAccessor<TItem>.ResolvePath(Property);
        if (HasField)
            Accessor = BitDataGridPropertyAccessor<TItem>.For(EffectiveField!);

        // Only record a registered id when the grid actually accepted this column. A duplicate-id
        // column is skipped by AddColumn (returns false); treating it as registered would let
        // OnParametersSet later re-key a column that was never in the grid, and would suppress the
        // retry below once the id becomes unique.
        _registeredId = Grid.AddColumn(this) ? Id : null;
        SnapshotSemanticParameters();
        _lastVisible = Visible;
        _lastFrozen = Frozen;
        _lastFrozenEnd = FrozenEnd;
    }

    protected override void OnParametersSet()
    {
        _propertyPath = Property is null ? null : BitDataGridPropertyAccessor<TItem>.ResolvePath(Property);
        if (HasField)
            Accessor = BitDataGridPropertyAccessor<TItem>.For(EffectiveField!);
        else
            Accessor = null;

        // The grid caches its visible-column list, so a Visible change arriving as a parameter (not via
        // the grid's own column chooser) must invalidate that cache or the layout goes stale.
        if (_lastVisible != Visible)
        {
            _lastVisible = Visible;
            if (_registeredId is not null) Grid?.NotifyColumnVisibilityChanged();
        }

        // Frozen/FrozenEnd changes go through the same layout invalidation: sticky offsets and the
        // cached column slots (which always render sticky columns) are computed from them.
        if (_lastFrozen != Frozen || _lastFrozenEnd != FrozenEnd)
        {
            _lastFrozen = Frozen;
            _lastFrozenEnd = FrozenEnd;
            if (_registeredId is not null) Grid?.NotifyColumnVisibilityChanged();
        }

        // This column was skipped during initial registration because its id collided with another
        // column (AddColumn returned false, leaving _registeredId null). Now that ColumnId/Field may
        // have changed to a unique value, retry the registration so it can finally join the grid.
        if (_registeredId is null)
        {
            if (Grid?.AddColumn(this) == true)
            {
                _registeredId = Id;
                SnapshotSemanticParameters();
            }
            return;
        }

        // ColumnId/Field are mutable parameters, so the resolved Id may have changed since the column
        // was registered. Re-register under the new id (migrating any active descriptors) so grid
        // lookups by id keep finding this column.
        if (_registeredId is not null && _registeredId != Id)
        {
            Grid?.UpdateColumnRegistration(this, _registeredId);
            _registeredId = Id;
            // UpdateColumnRegistration already refreshes the grid; resync the snapshot so the change
            // detection below doesn't trigger a second redundant refresh in the same parameter set.
            SnapshotSemanticParameters();
        }
        else if (_registeredId is not null && SemanticParametersChanged())
        {
            // Field/Aggregate/Format/AggregateFormat changed while the Id stayed the same (typically a
            // fixed ColumnId with a mutated Field). The grid resolves accessors/aggregates by column,
            // so ask it to recompute its view so the active state doesn't go stale.
            Grid?.NotifyColumnChanged();
            SnapshotSemanticParameters();
        }
    }

    private bool SemanticParametersChanged()
        => _lastField != EffectiveField
        || _lastAggregate != Aggregate
        || _lastFormat != Format
        || _lastAggregateFormat != AggregateFormat;

    private void SnapshotSemanticParameters()
    {
        _lastField = EffectiveField;
        _lastAggregate = Aggregate;
        _lastFormat = Format;
        _lastAggregateFormat = AggregateFormat;
    }

    public void Dispose()
    {
        // Only unregister when this column was actually accepted by Grid.AddColumn (_registeredId set).
        // A duplicate-id column that was skipped never owns a registry entry, so calling RemoveColumn
        // for it would needlessly probe the grid for a column it never held.
        if (_registeredId is not null)
            Grid?.RemoveColumn(this);
    }

    internal object? GetValue(TItem item) => Accessor?.GetValue(item);

    internal string GetFormattedValue(TItem item)
    {
        var value = GetValue(item);
        return FormatValue(value);
    }

    internal string FormatValue(object? value)
    {
        if (value is null) return string.Empty;
        if (!string.IsNullOrEmpty(Format) && value is IFormattable f)
            return f.ToString(Format, System.Globalization.CultureInfo.CurrentCulture);
        return value.ToString() ?? string.Empty;
    }

    private static string? Humanize(string? field)
    {
        if (string.IsNullOrWhiteSpace(field)) return null;
        var name = field.Contains('.') ? field[(field.LastIndexOf('.') + 1)..] : field;
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpperInvariant(c) : c);
        }
        return sb.ToString();
    }
}
