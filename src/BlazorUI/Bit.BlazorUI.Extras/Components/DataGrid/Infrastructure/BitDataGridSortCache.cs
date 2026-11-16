using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// One level of a sort: the column it sorts by, which way, the key it reads off a row (the column's SortBy, else its
/// field), the column's own comparer if it has one, and the keys of the rows, read once, in the rows' order.
/// </summary>
internal readonly record struct BitDataGridSortLevel<TItem>(string ColumnId, bool Descending, Func<TItem, object?> Key, IComparer<object?>? ColumnComparer, object?[] Keys);

/// <summary>
/// The last sort of a grid's client-side view, handed back when a refresh sorts the same rows by the same keys again.
/// </summary>
/// <remarks>
/// <para>
/// A grid over a list refreshes on every render of its parent, since the parent may have changed the list in place
/// (the same instance, rows added, removed, moved or edited), and a sort is the dear part of a refresh: most of a
/// second for 100,000 rows on WebAssembly. A click on a sort that the parent re-renders after (an OnSortChange, a
/// status line) sorted the whole view twice.
/// </para>
/// <para>
/// The sort is stable, so the same rows in the same order, sorted by keys that compare the same, come out the same.
/// The keys are read again on every refresh - a row edited in place has the key it has now - and the last result is
/// handed back only when everything the sort reads matches: the same rows (the same instances, for a class) in the same
/// order, the same levels (column, direction, key selector), and every key equal to the one it had. Equality stands for
/// "compares the same" only for keys of the comparer's trusted types (BitDataGridValueComparer.IsTrustedComparable),
/// which are immutable values, and only under the comparer the grid picks itself: a sort with a key of any other type,
/// or by a column with a Comparer of its own (which may read state of its own), is never handed back.
/// </para>
/// </remarks>
internal sealed class BitDataGridSortCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>
{
    private TItem[]? _rows;
    private List<BitDataGridSortLevel<TItem>>? _levels;
    private IReadOnlyList<TItem>? _sorted;

    public IReadOnlyList<TItem> Sort(IReadOnlyList<TItem> source, IReadOnlyList<BitDataGridSortDescriptor> sorts, IReadOnlyDictionary<string, BitDataGridColumn<TItem>> columns)
    {
        var levels = BitDataGridDataProcessor.SortLevels(source, sorts, columns);
        if (levels.Count == 0)
        {
            Clear();
            return source;
        }

        if (_sorted is not null && SameRows(source) && SameLevels(levels)) return _sorted;

        var sorted = BitDataGridDataProcessor.SortByLevels(source, levels);
        if (CanBeHandedBack(levels))
        {
            _rows = [.. source];
            _levels = levels;
            _sorted = sorted;
        }
        else
        {
            Clear();
        }
        return sorted;
    }

    /// <summary>Lets go of the last sort, and the rows it holds.</summary>
    public void Clear()
    {
        _rows = null;
        _levels = null;
        _sorted = null;
    }

    private bool SameRows(IReadOnlyList<TItem> source)
    {
        var rows = _rows!;
        if (rows.Length != source.Count) return false;

        if (typeof(TItem).IsValueType)
        {
            // A struct row is a copy: an equal one is the same row as far as the view goes.
            var comparer = EqualityComparer<TItem>.Default;
            for (var i = 0; i < rows.Length; i++)
            {
                if (comparer.Equals(rows[i], source[i]) is false) return false;
            }
            return true;
        }

        // A class row is the same row only as the same instance: the view hands the instances on (to the cells, the
        // selection, the editors), so a new instance equal to the old one still has to take its place.
        for (var i = 0; i < rows.Length; i++)
        {
            if (ReferenceEquals(rows[i], source[i]) is false) return false;
        }
        return true;
    }

    private bool SameLevels(List<BitDataGridSortLevel<TItem>> levels)
    {
        var last = _levels!;
        if (last.Count != levels.Count) return false;

        for (var l = 0; l < levels.Count; l++)
        {
            var (before, now) = (last[l], levels[l]);
            if (before.ColumnId != now.ColumnId || before.Descending != now.Descending || before.Key.Equals(now.Key) is false) return false;

            for (var i = 0; i < now.Keys.Length; i++)
            {
                if (Equals(before.Keys[i], now.Keys[i]) is false) return false;
            }
        }
        return true;
    }

    private static bool CanBeHandedBack(List<BitDataGridSortLevel<TItem>> levels)
    {
        foreach (var level in levels)
        {
            if (level.ColumnComparer is not null) return false;

            Type? trusted = null;
            foreach (var key in level.Keys)
            {
                if (key is null) continue;

                var type = key.GetType();
                if (type == trusted) continue;
                if (BitDataGridValueComparer.IsTrustedComparable(type) is false) return false;
                trusted = type;
            }
        }
        return true;
    }
}
