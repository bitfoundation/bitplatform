namespace Bit.BlazorUI;

/// <summary>
/// A Fenwick (binary indexed) tree specialized for virtual scrolling.
/// <para>
/// Each leaf holds the size (height or width) of a single item. The tree answers two
/// questions that virtualization needs on every scroll frame, both in O(log n):
/// </para>
/// <list type="bullet">
///   <item><see cref="PrefixSum"/> - the cumulative offset of an item (sum of all sizes before it).</item>
///   <item><see cref="FindIndex"/> - the index of the item that occupies a given scroll offset.</item>
/// </list>
/// Sizes can be updated individually in O(log n) as items are measured, which keeps
/// dynamic-size virtualization cheap even for very large lists.
/// </summary>
internal sealed class BitVirtualizePrefixSumTree
{
    private double[] _tree;   // 1-indexed Fenwick storage
    private double[] _values; // 0-indexed current size of each item
    private int _count;
    private double _total;

    public BitVirtualizePrefixSumTree(int count, double defaultValue)
    {
        _count = count;
        _values = new double[count];
        _tree = new double[count + 1];
        Reset(count, defaultValue);
    }

    /// <summary>
    /// Number of items tracked by the tree.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Sum of every item size (the full scrollable extent).
    /// </summary>
    public double Total => _total;

    /// <summary>
    /// Resizes the tree to <paramref name="count"/> items, seeding every size to <paramref name="defaultValue"/>.
    /// </summary>
    public void Reset(int count, double defaultValue)
    {
        _count = count;
        if (_values.Length < count)
        {
            _values = new double[count];
            _tree = new double[count + 1];
        }
        else
        {
            Array.Clear(_tree, 0, _count + 1);
        }

        for (var i = 0; i < count; i++)
        {
            _values[i] = defaultValue;
        }

        // Build the Fenwick tree in O(n) rather than n * O(log n).
        for (var i = 1; i <= count; i++)
        {
            _tree[i] += defaultValue;
            var parent = i + (i & -i);
            if (parent <= count)
            {
                _tree[parent] += _tree[i];
            }
        }

        _total = defaultValue * count;
    }

    /// <summary>
    /// Resizes the tree to <paramref name="count"/> items, preserving the sizes of the surviving
    /// indices and seeding only the newly added items with <paramref name="defaultValue"/>.
    /// </summary>
    public void Resize(int count, double defaultValue)
    {
        var oldCount = _count;
        _count = count;

        if (_values.Length < count)
        {
            Array.Resize(ref _values, count);
            _tree = new double[count + 1];
        }
        else
        {
            Array.Clear(_tree, 0, count + 1);
        }

        _total = 0d;
        for (var i = 0; i < count; i++)
        {
            if (i >= oldCount)
            {
                _values[i] = defaultValue;
            }

            _total += _values[i];
        }

        // Build the Fenwick tree in O(n) rather than n * O(log n).
        for (var i = 1; i <= count; i++)
        {
            _tree[i] += _values[i - 1];
            var parent = i + (i & -i);
            if (parent <= count)
            {
                _tree[parent] += _tree[i];
            }
        }
    }

    /// <summary>
    /// Gets the currently stored size for <paramref name="index"/>.
    /// </summary>
    public double GetSize(int index) => _values[index];

    /// <summary>
    /// Sets the size of <paramref name="index"/> to <paramref name="value"/>; returns the delta applied.
    /// </summary>
    public double SetSize(int index, double value)
    {
        var delta = value - _values[index];
        if (delta == 0d) return 0d;

        _values[index] = value;
        _total += delta;

        for (var i = index + 1; i <= _count; i += i & -i)
        {
            _tree[i] += delta;
        }

        return delta;
    }

    /// <summary>
    /// Returns the cumulative offset of the item at <paramref name="index"/>:
    /// the sum of the sizes of items <c>[0, index)</c>.
    /// </summary>
    public double PrefixSum(int index)
    {
        if (index <= 0) return 0d;

        if (index > _count)
        {
            index = _count;
        }

        var sum = 0d;
        for (var i = index; i > 0; i -= i & -i)
        {
            sum += _tree[i];
        }

        return sum;
    }

    /// <summary>
    /// Returns the zero-based index of the item that contains the scroll offset
    /// <paramref name="offset"/> (i.e. the largest index whose cumulative offset is &lt;= <paramref name="offset"/>).
    /// </summary>
    public int FindIndex(double offset)
    {
        if (offset <= 0d || _count == 0) return 0;

        if (offset >= _total) return _count - 1;

        var pos = 0;
        var remaining = offset;

        // Largest power of two <= _count.
        var pw = 1;
        while (pw <= _count >> 1)
        {
            pw <<= 1;
        }

        for (; pw > 0; pw >>= 1)
        {
            var next = pos + pw;
            if (next <= _count && _tree[next] <= remaining)
            {
                pos = next;
                remaining -= _tree[pos];
            }
        }

        // pos == number of items whose cumulative size is <= offset == index of the item at offset.
        return pos >= _count ? _count - 1 : pos;
    }
}
