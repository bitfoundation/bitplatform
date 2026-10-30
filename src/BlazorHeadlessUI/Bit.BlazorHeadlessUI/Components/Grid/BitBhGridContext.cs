namespace Bit.BlazorHeadlessUI;

/// <summary>State of a <see cref="BitBhGrid"/> cascaded to rows and cells.</summary>
public sealed class BitBhGridContext
{
    private Dictionary<string, BitBhGridRow>? _rowsByKey;

    internal BitBhGridContext(BitBhGrid grid, string key)
    {
        Grid = grid;
        Rows = new BitBhCollection<BitBhGridRow>(key);
        Rows.Changed += () => _rowsByKey = null;
    }

    internal BitBhGrid Grid { get; }

    /// <summary>The registered rows in document order.</summary>
    public BitBhCollection<BitBhGridRow> Rows { get; }

    /// <summary>The cell holding the tab stop.</summary>
    public BitBhGridCell? FocusedCell { get; private set; }

    /// <summary>Whether the grid is a treegrid.</summary>
    public bool IsTreeGrid => Grid.TreeGrid;

    /// <summary>The row selection mode.</summary>
    public BitBhGridSelectionMode SelectionMode => Grid.SelectionMode;

    /// <summary>Whether the row with <paramref name="key"/> is selected.</summary>
    public bool IsSelected(string? key) => Grid.IsSelected(key);

    /// <summary>Whether the row with <paramref name="key"/> is expanded.</summary>
    public bool IsExpanded(string? key) => Grid.IsExpanded(key);

    /// <summary>
    /// Whether <paramref name="row"/> is shown: in a treegrid, rows whose ancestor
    /// (via <see cref="BitBhGridRow.ParentKey"/>) is collapsed are hidden.
    /// </summary>
    public bool IsRowVisible(BitBhGridRow row)
    {
        if (!IsTreeGrid || row.ParentKey is null) return true;
        _rowsByKey ??= Rows.Items.Where(r => r.Key is not null).GroupBy(r => r.Key!).ToDictionary(g => g.Key, g => g.First());
        var parentKey = row.ParentKey;
        for (var depth = 0; parentKey is not null && depth < 256; depth++)
        {
            if (!IsExpanded(parentKey)) return false;
            parentKey = _rowsByKey.TryGetValue(parentKey, out var parent) ? parent.ParentKey : null;
        }
        return true;
    }

    /// <summary>Whether <paramref name="row"/> is below the row with <paramref name="ancestorKey"/> (treegrid).</summary>
    internal bool IsDescendantOf(BitBhGridRow row, string ancestorKey)
    {
        _rowsByKey ??= Rows.Items.Where(r => r.Key is not null).GroupBy(r => r.Key!).ToDictionary(g => g.Key, g => g.First());
        var parentKey = row.ParentKey;
        for (var depth = 0; parentKey is not null && depth < 256; depth++)
        {
            if (parentKey == ancestorKey) return true;
            parentKey = _rowsByKey.TryGetValue(parentKey, out var parent) ? parent.ParentKey : null;
        }
        return false;
    }

    /// <summary>The visible rows in document order.</summary>
    internal List<BitBhGridRow> VisibleRows() => Rows.Items.Where(IsRowVisible).ToList();

    internal bool IsTabStop(BitBhGridCell cell)
    {
        if (FocusedCell is not null && FocusedCell.Row is { IsRegistered: true } row && row.IndexOfCell(FocusedCell) >= 0 && IsRowVisible(row))
            return ReferenceEquals(FocusedCell, cell);
        // A cell that asked to be the initial tab stop wins.
        var preferred = Rows.Items.Where(IsRowVisible).SelectMany(r => r.Cells).FirstOrDefault(c => c.InitialTabStop);
        if (preferred is not null) return ReferenceEquals(preferred, cell);
        // Default: the first cell of the first visible body row (or the first row).
        var first = Rows.Items.FirstOrDefault(r => !r.IsHeaderRow && r.Cells.Count > 0 && IsRowVisible(r)) ?? Rows.Items.FirstOrDefault(r => r.Cells.Count > 0);
        return first is not null && ReferenceEquals(first.Cells[0], cell);
    }

    /// <summary>Re-renders every cell (their tab stop may have moved).</summary>
    internal void RefreshCells()
    {
        foreach (var row in Rows.Items)
            foreach (var c in row.Cells) c.Refresh();
    }

    internal void SetFocus(BitBhGridCell cell)
    {
        if (ReferenceEquals(FocusedCell, cell)) return;
        var previous = FocusedCell;
        FocusedCell = cell;
        previous?.Refresh();
        cell.Refresh();
        if (previous is null)
            foreach (var row in Rows.Items) foreach (var c in row.Cells) if (!ReferenceEquals(c, cell)) c.Refresh();
    }
}
