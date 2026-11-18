namespace Bit.Bmage;

/// <summary>A growable run of glyphs being shaped.</summary>
internal sealed class BmageGlyphBuffer
{
    /// <summary>A bound on how far substitutions may grow a run, so a hostile font cannot exhaust memory.</summary>
    public const int MaxLength = 16384;

    private BmageShapingGlyph[] _items = new BmageShapingGlyph[16];
    private int _nextLigatureId;
    private int[] _bases = [];

    public int Count { get; private set; }

    public ref BmageShapingGlyph this[int index] => ref _items[index];

    public Span<BmageShapingGlyph> AsSpan() => _items.AsSpan(0, Count);

    public int NewLigatureId() => ++_nextLigatureId;

    public void Add(BmageShapingGlyph glyph)
    {
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        _items[Count++] = glyph;
    }

    public bool Insert(int index, BmageShapingGlyph glyph)
    {
        if (Count >= MaxLength) return false;
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        Array.Copy(_items, index, _items, index + 1, Count - index);
        _items[index] = glyph;
        Count++;
        return true;
    }

    public void RemoveAt(int index)
    {
        Array.Copy(_items, index + 1, _items, index, Count - index - 1);
        Count--;
    }

    public void FindBases()
    {
        if (_bases.Length < Count) _bases = new int[Count];
        var last = -1;
        for (var i = 0; i < Count; i++)
        {
            _bases[i] = last;
            if (_items[i].GlyphClass != 3 && !_items[i].Hidden) last = i;
        }
    }

    public int BaseBefore(int index) => _bases[index];
}
