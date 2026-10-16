namespace Bit.BevTools;

/// <summary>Fixed-capacity FIFO buffer. Thread-safe; snapshots allocate one array.</summary>
internal sealed class RingBuffer<T>
{
    private readonly object _lock = new();
    private readonly T[] _items;
    private int _head; // index of oldest
    private int _count;
    private long _version;

    public RingBuffer(int capacity)
    {
        _items = new T[Math.Max(1, capacity)];
    }

    public int Capacity => _items.Length;

    public int Count
    {
        get { lock (_lock) { return _count; } }
    }

    public long Version => Volatile.Read(ref _version);

    public void Add(T item) => Add(item, out _);

    /// <summary>Adds an item and reports the item evicted when the buffer was already full.</summary>
    public bool Add(T item, out T? evicted)
    {
        lock (_lock)
        {
            var wasFull = _count == _items.Length;
            evicted = wasFull ? _items[_head] : default;
            if (_count < _items.Length)
            {
                _items[(_head + _count) % _items.Length] = item;
                _count++;
            }
            else
            {
                _items[_head] = item;
                _head = (_head + 1) % _items.Length;
            }

            _version++;
            return wasFull;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_items);
            _head = 0;
            _count = 0;
            _version++;
        }
    }

    public T[] ToArray()
    {
        lock (_lock)
        {
            var result = new T[_count];
            for (var i = 0; i < _count; i++)
            {
                result[i] = _items[(_head + i) % _items.Length];
            }

            return result;
        }
    }

    /// <summary>Returns the newest item or default when empty.</summary>
    public T? Last
    {
        get
        {
            lock (_lock)
            {
                return _count == 0 ? default : _items[(_head + _count - 1) % _items.Length];
            }
        }
    }

    /// <summary>
    /// Finds an item by key, newest first. Concurrent producers can reserve monotonic ids and insert out of order,
    /// so binary search would occasionally miss a record.
    /// </summary>
    public T? FindByKey(Func<T, long> key, long value)
    {
        lock (_lock)
        {
            for (var i = _count - 1; i >= 0; i--)
            {
                var item = _items[(_head + i) % _items.Length];
                if (key(item) == value)
                {
                    return item;
                }
            }

            return default;
        }
    }

    /// <summary>Atomically replaces an item identified by key.</summary>
    public bool TryReplaceByKey(Func<T, long> key, long value, Func<T, T> replace)
    {
        lock (_lock)
        {
            for (var i = _count - 1; i >= 0; i--)
            {
                var index = (_head + i) % _items.Length;
                var item = _items[index];
                if (key(item) == value)
                {
                    _items[index] = replace(item);
                    _version++;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Finds the newest item matching a predicate without allocating.</summary>
    public T? FindLast(Func<T, bool> predicate)
    {
        lock (_lock)
        {
            for (var i = _count - 1; i >= 0; i--)
            {
                var item = _items[(_head + i) % _items.Length];
                if (predicate(item))
                {
                    return item;
                }
            }

            return default;
        }
    }
}
