using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bit.Bink;

/// <summary>
/// What a browser surface holds on behalf of the engine: paths it has built into <c>Path2D</c> objects and long
/// strings (image URIs, long text) it has kept, each under a small numeric slot.
/// </summary>
/// <remarks>
/// <para>
/// Without it every redraw re-sent every visible path coordinate by coordinate and replayed it one canvas call at
/// a time. A five-thousand-stroke page was millions of numbers per redraw, about fifty megabytes of JSON over a
/// Blazor Server circuit, and an embedded photo travelled again with every stroke drawn on top of it. With it a
/// path crosses the boundary once and a redraw refers to it by slot: a handful of numbers per element.
/// </para>
/// <para>
/// Identity is by reference. Elements build their geometry once and share it between versions, so a moved stroke
/// keeps its slot, while an edited one gets a new path and therefore a new slot. Entries not drawn for a while are
/// released, least recently used first, once the cache holds more than twice what the last frame drew.
/// </para>
/// <para>
/// This side is authoritative: the browser only ever uses a slot after the command that defined it, in the same
/// ordered stream. If the browser nevertheless finds a slot missing it says so, and <see cref="Clear"/> starts both
/// sides over with a full redraw.
/// </para>
/// </remarks>
internal sealed class BinkCanvasResourceCache
{
    /// <summary>Strings shorter than this travel in the frame's own string table; only longer ones are kept.</summary>
    public const int RetainedStringLength = 512;

    private readonly Pool<BinkPathGeometry> _paths = new(4096);
    private readonly Pool<string> _strings = new(32);
    private long _frame;

    /// <summary>Paths currently held by the browser.</summary>
    public int PathCount => _paths.Count;

    /// <summary>Strings currently held by the browser.</summary>
    public int StringCount => _strings.Count;

    /// <summary>Starts recording a frame.</summary>
    public void BeginFrame() => _frame++;

    /// <summary>Returns the slot of a path the browser already holds, marking it used this frame.</summary>
    public bool TryUsePath(BinkPathGeometry path, out int slot) => _paths.TryUse(path, _frame, out slot);

    /// <summary>Records that the browser is about to hold a path, and returns its slot.</summary>
    public int AddPath(BinkPathGeometry path) => _paths.Add(path, _frame);

    /// <summary>Returns the slot of a string the browser already holds, marking it used this frame.</summary>
    public bool TryUseString(string value, out int slot) => _strings.TryUse(value, _frame, out slot);

    /// <summary>Records that the browser is about to hold a string, and returns its slot.</summary>
    public int AddString(string value) => _strings.Add(value, _frame);

    /// <summary>Releases entries the frame just recorded did not use, when the cache has grown past its budget.</summary>
    public void EndFrame(List<int> freedPaths, List<int> freedStrings)
    {
        _paths.Evict(_frame, freedPaths);
        _strings.Evict(_frame, freedStrings);
    }

    /// <summary>Forgets everything, so the next frame defines whatever it draws.</summary>
    public void Clear()
    {
        _paths.Clear();
        _strings.Clear();
    }

    private sealed class Pool<T>(int minimumCapacity) where T : class
    {
        private readonly Dictionary<T, Entry> _entries = new(ReferenceEqualityComparer.Instance);
        private readonly Stack<int> _free = new();
        private int _next;
        private int _usedThisFrame;

        private struct Entry
        {
            public int Slot;
            public long LastUsed;
        }

        public int Count => _entries.Count;

        public bool TryUse(T key, long frame, out int slot)
        {
            ref Entry entry = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, key);
            if (Unsafe.IsNullRef(ref entry))
            {
                slot = -1;
                return false;
            }
            if (entry.LastUsed != frame)
            {
                entry.LastUsed = frame;
                _usedThisFrame++;
            }
            slot = entry.Slot;
            return true;
        }

        public int Add(T key, long frame)
        {
            // Freed slots are reused so slot numbers stay small: they travel as 32-bit floats, which are exact only
            // up to 2^24, and a session that keeps drawing would otherwise count past that.
            int slot = _free.Count > 0 ? _free.Pop() : _next++;
            _entries[key] = new Entry { Slot = slot, LastUsed = frame };
            _usedThisFrame++;
            return slot;
        }

        public void Evict(long frame, List<int> freed)
        {
            int used = _usedThisFrame;
            _usedThisFrame = 0;
            int capacity = Math.Max(minimumCapacity, used * 2);
            if (_entries.Count <= capacity) return;

            // Released down to three quarters of the budget, so a cache hovering at the limit is not swept on every
            // frame. Nothing the frame just drew is a candidate.
            var candidates = new List<(long LastUsed, T Key)>(_entries.Count - used);
            foreach ((T key, Entry entry) in _entries)
            {
                if (entry.LastUsed != frame) candidates.Add((entry.LastUsed, key));
            }
            candidates.Sort(static (a, b) => a.LastUsed.CompareTo(b.LastUsed));
            int excess = _entries.Count - Math.Max(capacity * 3 / 4, used);
            for (int i = 0; i < candidates.Count && excess > 0; i++, excess--)
            {
                T key = candidates[i].Key;
                int slot = _entries[key].Slot;
                _entries.Remove(key);
                _free.Push(slot);
                freed.Add(slot);
            }
        }

        public void Clear()
        {
            _entries.Clear();
            _free.Clear();
            _next = 0;
            _usedThisFrame = 0;
        }
    }
}
