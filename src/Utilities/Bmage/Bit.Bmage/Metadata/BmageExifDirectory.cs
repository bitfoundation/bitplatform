namespace Bit.Bmage;

/// <summary>A single image file directory: an ordered map of tag → value.</summary>
public sealed class BmageExifDirectory
{
    private readonly SortedDictionary<ushort, BmageExifValue> _tags;

    public BmageExifDirectory() : this(new SortedDictionary<ushort, BmageExifValue>()) { }

    public BmageExifDirectory(IDictionary<ushort, BmageExifValue> tags) => _tags = new SortedDictionary<ushort, BmageExifValue>(tags);

    public IReadOnlyDictionary<ushort, BmageExifValue> Tags => _tags;
    public int Count => _tags.Count;

    public BmageExifValue? Get(ushort tag) => _tags.TryGetValue(tag, out var v) ? v : null;
    public bool Contains(ushort tag) => _tags.ContainsKey(tag);

    /// <summary>Returns a copy with the tag set (or removed when <paramref name="value"/> is null).</summary>
    public BmageExifDirectory With(ushort tag, BmageExifValue? value)
    {
        var copy = new BmageExifDirectory(_tags);
        if (value is null) copy._tags.Remove(tag); else copy._tags[tag] = value;
        return copy;
    }

    /// <summary>Returns a copy without the listed tags.</summary>
    public BmageExifDirectory Without(params ushort[] tags)
    {
        var copy = new BmageExifDirectory(_tags);
        foreach (var t in tags) copy._tags.Remove(t);
        return copy;
    }
}
