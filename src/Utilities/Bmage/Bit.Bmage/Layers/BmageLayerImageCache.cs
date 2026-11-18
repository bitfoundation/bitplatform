using System.Buffers;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// The images image layers are drawn with, shrunk and run through their pipeline once and kept from one composite to
/// the next: dragging a layer's opacity or moving it blends it again without preparing its image again. Used by one
/// composite at a time; it relies on a layer's image not changing in place, as an edit session does.
/// </summary>
internal sealed class BmageLayerImageCache
{
    // The image, its pipeline and filter, and the scale it is shrunk by: the same instances and scale mean the same pixels.
    private readonly record struct Key(BmageImageBuffer Image, BmageImagePipeline Pipeline, BmageResamplingFilter Filter, double Scale);

    private sealed class Entry(BmageImageBuffer pixels)
    {
        public BmageImageBuffer Pixels { get; } = pixels;
        public int Composite { get; set; }
    }

    private readonly Dictionary<Key, Entry> _entries = [];
    private int _composite;

    /// <summary>Starts a composite: what it does not use is released at its <see cref="End"/>.</summary>
    public void Begin() => _composite++;

    /// <summary>The prepared image of <paramref name="layer"/>, prepared now unless an earlier composite kept it. The cache owns it.</summary>
    public BmageImageBuffer GetOrAdd(BmageImageLayer layer, BmageOperationContext context)
    {
        var key = new Key(layer.Image, layer.Pipeline, layer.Filter, layer.PrepareScale());
        if (!_entries.TryGetValue(key, out var entry))
        {
            entry = new Entry(layer.PrepareImage(context));
            _entries[key] = entry;
        }
        entry.Composite = _composite;
        return entry.Pixels;
    }

    /// <summary>Ends a composite, releasing the images it did not draw. A composite that failed leaves everything for the next one.</summary>
    public void End()
    {
        foreach (var (key, entry) in _entries.Where(e => e.Value.Composite != _composite).ToArray())
        {
            entry.Pixels.Dispose();
            _entries.Remove(key);
        }
    }

    /// <summary>Releases every image.</summary>
    public void Clear()
    {
        foreach (var entry in _entries.Values) entry.Pixels.Dispose();
        _entries.Clear();
    }
}
