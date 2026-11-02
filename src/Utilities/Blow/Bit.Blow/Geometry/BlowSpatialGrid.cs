namespace Bit.Blow;

/// <summary>
/// Items bucketed by where their boxes lie on a uniform grid, to find the ones near an area without testing every
/// one: the nodes in the way of each routed edge, asked for on every frame of a drag.
/// </summary>
internal sealed class BlowSpatialGrid<T>
{
    /// <summary>The side of a cell, in flow units: a few ordinary nodes across.</summary>
    private const double CellSize = 256;

    /// <summary>A box over more cells than this - a big group - is not bucketed but offered for every area.</summary>
    private const int MaxCellsPerItem = 64;

    private readonly List<T> _items = [];
    private readonly Dictionary<(int X, int Y), List<int>> _cells = [];
    private readonly List<int> _everywhere = [];

    public void Add(T item, BlowRect box)
    {
        var at = _items.Count;
        _items.Add(item);

        if (!Range(box, out var x0, out var y0, out var x1, out var y1) || (long)(x1 - x0 + 1) * (y1 - y0 + 1) > MaxCellsPerItem)
        {
            _everywhere.Add(at);
            return;
        }

        for (var x = x0; x <= x1; x++)
        {
            for (var y = y0; y <= y1; y++)
            {
                if (!_cells.TryGetValue((x, y), out var cell)) _cells[(x, y)] = cell = [];
                cell.Add(at);
            }
        }
    }

    /// <summary>
    /// The items whose box may meet <paramref name="area"/>, in the order they were added - more than meet it, never
    /// fewer: the caller tests each.
    /// </summary>
    public IEnumerable<T> Near(BlowRect area)
    {
        // An area over more cells than there are items is quicker to answer with all of them.
        if (!Range(area, out var x0, out var y0, out var x1, out var y1) || (long)(x1 - x0 + 1) * (y1 - y0 + 1) > _items.Count)
            return _items;

        var found = new List<int>(_everywhere);
        for (var x = x0; x <= x1; x++)
        {
            for (var y = y0; y <= y1; y++)
            {
                if (_cells.TryGetValue((x, y), out var cell)) found.AddRange(cell);
            }
        }
        found.Sort();

        var near = new List<T>(found.Count);
        for (var i = 0; i < found.Count; i++)
        {
            if (i == 0 || found[i] != found[i - 1]) near.Add(_items[found[i]]);
        }
        return near;
    }

    /// <summary>The cells a box covers; false for a box with no finite place to be bucketed by.</summary>
    private static bool Range(BlowRect box, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = y0 = x1 = y1 = 0;
        if (!double.IsFinite(box.Left) || !double.IsFinite(box.Top) || !double.IsFinite(box.Right) || !double.IsFinite(box.Bottom))
            return false;

        x0 = Cell(box.Left);
        y0 = Cell(box.Top);
        x1 = Cell(box.Right);
        y1 = Cell(box.Bottom);
        return true;

        static int Cell(double v) => (int)Math.Clamp(Math.Floor(v / CellSize), -1_000_000_000d, 1_000_000_000d);
    }
}
