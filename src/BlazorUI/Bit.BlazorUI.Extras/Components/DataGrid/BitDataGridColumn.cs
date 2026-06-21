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

    /// <summary>Name of the property this column is bound to. Supports nested paths ("Address.City").</summary>
    [Parameter] public string? Field { get; set; }

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
    /// When true, the first click on the header sorts descending instead of ascending.
    /// Mirrors react-data-grid's <c>sortDescendingFirst</c>.
    /// </summary>
    [Parameter] public bool SortDescendingFirst { get; set; }
    [Parameter] public bool? Filterable { get; set; }
    [Parameter] public bool? Resizable { get; set; }
    [Parameter] public bool? Reorderable { get; set; }
    [Parameter] public bool? Editable { get; set; }
    [Parameter] public bool? Groupable { get; set; }

    /// <summary>Pin the column to the start edge so it stays visible while scrolling horizontally.</summary>
    [Parameter] public bool Frozen { get; set; }

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

    internal BitDataGridPropertyAccessor<TItem>? Accessor { get; private set; }

    internal string Id => ColumnId ?? Field ?? $"col-{GetHashCode():x}";

    internal string DisplayTitle => Title ?? Humanize(Field) ?? Id;

    internal bool HasField => !string.IsNullOrEmpty(Field);

    internal BitDataGridColumnDataType EffectiveDataType
    {
        get
        {
            if (DataType != BitDataGridColumnDataType.Auto) return DataType;
            if (Accessor is null) return BitDataGridColumnDataType.Text;
            var t = Accessor.UnderlyingType;
            if (t == typeof(bool)) return BitDataGridColumnDataType.Boolean;
            if (t.IsEnum) return BitDataGridColumnDataType.Enum;
            if (t == typeof(DateTime) || t == typeof(DateOnly) || t == typeof(DateTimeOffset)) return BitDataGridColumnDataType.Date;
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
                || t == typeof(double) || t == typeof(float) || t == typeof(decimal))
                return BitDataGridColumnDataType.Number;
            return BitDataGridColumnDataType.Text;
        }
    }

    protected override void OnInitialized()
    {
        if (Grid is null)
            throw new InvalidOperationException($"{nameof(BitDataGridColumn<TItem>)} must be used inside a {nameof(BitDataGrid<TItem>)}.");
        Grid.AddColumn(this);
    }

    protected override void OnParametersSet()
    {
        if (HasField)
            Accessor = BitDataGridPropertyAccessor<TItem>.For(Field!);
        else
            Accessor = null;
    }

    public void Dispose() => Grid?.RemoveColumn(this);

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
        if (string.IsNullOrEmpty(field)) return null;
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
