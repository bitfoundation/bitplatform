using System.Buffers.Binary;
using System.Text;

namespace Bit.Bmage.Codecs.Avif;

/// <summary>A property of an item (ISO/IEC 23008-12, the HEIF image file format): its type and payload.</summary>
internal readonly record struct HeifProperty(uint Type, int Offset, int Length);

/// <summary>One item of a HEIF file: an image, a derived grid, or metadata such as Exif.</summary>
internal sealed class HeifItem
{
    public uint Id;
    public uint Type;
    public string? ContentType;
    public int ConstructionMethod;
    public ulong BaseOffset;
    public readonly List<(ulong Offset, ulong Length)> Extents = [];
    public bool HasLocation;
    public readonly List<HeifProperty> Properties = [];
    /// <summary>References from this item, by type: dimg (grid tiles), auxl (master of an auxiliary), cdsc, prem.</summary>
    public readonly List<(uint Type, uint To)> References = [];
}

/// <summary>
/// Parses the ISO base media file format boxes of an AVIF file: ftyp, and in meta the hdlr, pitm, iloc, iinf, iref,
/// iprp and idat boxes, plus enough of moov to find the first frame of an image sequence. Every size and offset is
/// checked against the data, because the file is untrusted.
/// </summary>
internal sealed class AvifContainer
{
    public static uint FourCc(string s) => (uint)(s[0] << 24 | s[1] << 16 | s[2] << 8 | s[3]);

    public static readonly uint Av01 = FourCc("av01"), Grid = FourCc("grid"), Exif = FourCc("Exif"), Mime = FourCc("mime"),
        Ispe = FourCc("ispe"), Pixi = FourCc("pixi"), Av1C = FourCc("av1C"), Colr = FourCc("colr"), AuxC = FourCc("auxC"),
        Clap = FourCc("clap"), Irot = FourCc("irot"), Imir = FourCc("imir"), Dimg = FourCc("dimg"), Auxl = FourCc("auxl"),
        Cdsc = FourCc("cdsc"), Prem = FourCc("prem"), Nclx = FourCc("nclx"), Prof = FourCc("prof"), Ricc = FourCc("rICC");

    private readonly ReadOnlyMemory<byte> _data;
    /// <summary>The length of the whole file, which <see cref="ParseHeaders"/> holds only the boxes of.</summary>
    private readonly long _fileLength;
    private int _idatOffset = -1;
    private int _idatLength;

    public uint PrimaryItemId { get; private set; }
    public bool HasMeta { get; private set; }
    public readonly Dictionary<uint, HeifItem> Items = [];
    private readonly List<HeifProperty> _properties = [];

    /// <summary>The first sample of the first av01 track, for image sequences without a usable primary item.</summary>
    public (int Offset, int Length)? FirstTrackSample { get; private set; }
    public (int Offset, int Length)? TrackAv1C { get; private set; }
    /// <summary>The width and height of that track's av01 sample entry.</summary>
    public (int Width, int Height) TrackSize { get; private set; }

    private AvifContainer(ReadOnlyMemory<byte> data, long fileLength)
    {
        _data = data;
        _fileLength = fileLength;
    }

    /// <summary>The whole file, which the container reads in place.</summary>
    public ReadOnlyMemory<byte> Data => _data;

    public static AvifContainer Parse(ReadOnlyMemory<byte> data) => Parse(data, data.Length);

    private static AvifContainer Parse(ReadOnlyMemory<byte> data, long fileLength)
    {
        var c = new AvifContainer(data, fileLength);
        c.ParseTop();
        return c;
    }

    /// <summary>
    /// Parses a file held only as a span, for callers that read the file's structure and none of its items' data: its
    /// ftyp, meta and moov boxes are copied end to end, and none of the media data writers put before, between or after
    /// them.
    /// </summary>
    public static AvifContainer ParseHeaders(ReadOnlySpan<byte> data)
    {
        // What is read from the copy is either inside these boxes or, like a track sample's offset, a value checked
        // against the whole file's length, so the boxes left out change nothing but the size of the copy.
        var kept = new List<(int Start, int End)>();
        var size = 0;
        var start = 0;
        foreach (var (type, offset, length) in Boxes(data, 0, data.Length))
        {
            var end = offset + length;
            if (type == FourCc("ftyp") || type == FourCc("meta") || type == FourCc("moov"))
            {
                kept.Add((start, end));
                size += end - start;
            }
            start = end;
        }
        var headers = new byte[size];
        var pos = 0;
        foreach (var (boxStart, boxEnd) in kept)
        {
            data[boxStart..boxEnd].CopyTo(headers.AsSpan(pos));
            pos += boxEnd - boxStart;
        }
        return Parse(headers, data.Length);
    }

    private static ImageDecodeException Corrupt(string what) => new($"Invalid AVIF file: {what}.");

    /// <summary>Iterates boxes in [start, end): yields type, payload offset and payload length.</summary>
    private List<(uint Type, int Offset, int Length)> Boxes(int start, int end) => Boxes(_data.Span, start, end);

    private static List<(uint Type, int Offset, int Length)> Boxes(ReadOnlySpan<byte> data, int start, int end)
    {
        var list = new List<(uint, int, int)>();
        var pos = start;
        while (pos + 8 <= end)
        {
            long size = BinaryPrimitives.ReadUInt32BigEndian(data[pos..]);
            var type = BinaryPrimitives.ReadUInt32BigEndian(data[(pos + 4)..]);
            var header = 8;
            if (size == 1)
            {
                if (pos + 16 > end) throw Corrupt("truncated box");
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(data[(pos + 8)..]);
                header = 16;
            }
            else if (size == 0)
            {
                size = end - pos;
            }
            if (size < header || size > end - pos) throw Corrupt("box extends past its parent");
            list.Add((type, pos + header, (int)size - header));
            pos += (int)size;
            if (list.Count > 100_000) throw Corrupt("too many boxes");
        }
        return list;
    }

    private void ParseTop()
    {
        var sawFtyp = false;
        foreach (var (type, offset, length) in Boxes(0, _data.Length))
        {
            if (type == FourCc("ftyp"))
            {
                if (length < 8) throw Corrupt("short ftyp");
                sawFtyp = true;
            }
            else if (type == FourCc("meta"))
            {
                if (length < 4) throw Corrupt("short meta");
                HasMeta = true;
                ParseMeta(offset + 4, offset + length);
            }
            else if (type == FourCc("moov"))
            {
                ParseMoov(offset, offset + length);
            }
        }
        if (!sawFtyp) throw Corrupt("missing ftyp");
    }

    private void ParseMeta(int start, int end)
    {
        foreach (var (type, offset, length) in Boxes(start, end))
        {
            var span = _data.Span.Slice(offset, length);
            if (type == FourCc("pitm"))
            {
                if (length < 6) throw Corrupt("short pitm");
                PrimaryItemId = span[0] == 0 ? BinaryPrimitives.ReadUInt16BigEndian(span[4..]) : length >= 8 ? BinaryPrimitives.ReadUInt32BigEndian(span[4..]) : throw Corrupt("short pitm");
            }
            else if (type == FourCc("iloc")) ParseIloc(offset, length);
            else if (type == FourCc("iinf")) ParseIinf(offset, length);
            else if (type == FourCc("iref")) ParseIref(offset, length);
            else if (type == FourCc("iprp")) ParseIprp(offset, offset + length);
            else if (type == FourCc("idat"))
            {
                _idatOffset = offset;
                _idatLength = length;
            }
        }
    }

    private HeifItem Item(uint id)
    {
        if (!Items.TryGetValue(id, out var item))
        {
            if (Items.Count >= 65536) throw Corrupt("too many items");
            Items[id] = item = new HeifItem { Id = id };
        }
        return item;
    }

    private sealed class Cursor(ReadOnlyMemory<byte> data, int pos, int end)
    {
        public int Pos = pos;

        public ulong Read(int bytes)
        {
            if (bytes == 0) return 0;
            if (bytes > 8 || Pos + bytes > end) throw Corrupt("truncated box");
            var span = data.Span;
            ulong v = 0;
            for (var i = 0; i < bytes; i++) v = (v << 8) | span[Pos++];
            return v;
        }

        public uint U8() => (uint)Read(1);
        public uint U16() => (uint)Read(2);
        public uint U32() => (uint)Read(4);

        public string CString()
        {
            var span = data.Span;
            var start = Pos;
            while (Pos < end && span[Pos] != 0) Pos++;
            var s = Encoding.UTF8.GetString(span[start..Pos]);
            if (Pos < end) Pos++;
            return s;
        }

        public bool AtEnd => Pos >= end;
    }

    private void ParseIloc(int offset, int length)
    {
        var c = new Cursor(_data, offset, offset + length);
        var version = c.U8();
        c.Read(3);
        if (version > 2) throw Corrupt("unsupported iloc version");
        var sizes = c.U16();
        var offsetSize = (int)(sizes >> 12);
        var lengthSize = (int)((sizes >> 8) & 15);
        var baseOffsetSize = (int)((sizes >> 4) & 15);
        var indexSize = version is 1 or 2 ? (int)(sizes & 15) : 0;
        foreach (var s in new[] { offsetSize, lengthSize, baseOffsetSize, indexSize })
            if (s is not (0 or 4 or 8)) throw Corrupt("invalid iloc field size");
        var itemCount = version < 2 ? c.U16() : c.U32();
        for (var i = 0u; i < itemCount; i++)
        {
            var id = version < 2 ? c.U16() : c.U32();
            var item = Item(id);
            item.HasLocation = true;
            if (version is 1 or 2) item.ConstructionMethod = (int)(c.U16() & 15);
            // data_reference_index: 0 is this file; any other names a file an image cannot reach.
            if (c.U16() != 0) throw new ImageDecodeException("AVIF items stored in other files are not supported.");
            item.BaseOffset = c.Read(baseOffsetSize);
            var extentCount = c.U16();
            item.Extents.Clear();
            for (var e = 0; e < extentCount; e++)
            {
                if (indexSize > 0) c.Read(indexSize);
                var extentOffset = c.Read(offsetSize);
                var extentLength = c.Read(lengthSize);
                item.Extents.Add((extentOffset, extentLength));
            }
        }
    }

    private void ParseIinf(int offset, int length)
    {
        var c = new Cursor(_data, offset, offset + length);
        var version = c.U8();
        c.Read(3);
        var count = version == 0 ? c.U16() : c.U32();
        var start = c.Pos;
        foreach (var (type, boxOffset, boxLength) in Boxes(start, offset + length))
        {
            // entry_count infe boxes, whatever their version; any after them are not entries.
            if (count == 0) break;
            if (type != FourCc("infe")) continue;
            count--;
            var e = new Cursor(_data, boxOffset, boxOffset + boxLength);
            var infeVersion = e.U8();
            e.Read(3);
            if (infeVersion < 2) continue; // versions 0 and 1 carry no item type
            var id = infeVersion == 2 ? e.U16() : e.U32();
            e.U16(); // item_protection_index
            var item = Item(id);
            item.Type = e.U32();
            e.CString(); // item_name
            if (item.Type == Mime && !e.AtEnd) item.ContentType = e.CString();
        }
    }

    private void ParseIref(int offset, int length)
    {
        var version = _data.Span[offset];
        foreach (var (type, boxOffset, boxLength) in Boxes(offset + 4, offset + length))
        {
            var c = new Cursor(_data, boxOffset, boxOffset + boxLength);
            var from = version == 0 ? c.U16() : c.U32();
            var count = c.U16();
            var item = Item(from);
            for (var i = 0; i < count; i++) item.References.Add((type, version == 0 ? c.U16() : c.U32()));
        }
    }

    private void ParseIprp(int start, int end)
    {
        foreach (var (type, offset, length) in Boxes(start, end))
        {
            if (type == FourCc("ipco"))
            {
                foreach (var (propType, propOffset, propLength) in Boxes(offset, offset + length))
                    _properties.Add(new HeifProperty(propType, propOffset, propLength));
            }
            else if (type == FourCc("ipma"))
            {
                var c = new Cursor(_data, offset, offset + length);
                var version = c.U8();
                var flags = (uint)c.Read(3);
                var entryCount = c.U32();
                for (var i = 0u; i < entryCount; i++)
                {
                    var id = version < 1 ? c.U16() : c.U32();
                    var item = Item(id);
                    var associations = c.U8();
                    for (var a = 0; a < associations; a++)
                    {
                        // The top bit marks the property essential; every property this decoder reads is applied
                        // whether or not it is marked so.
                        var index = (flags & 1) != 0 ? (int)(c.U16() & 0x7FFF) : (int)(c.U8() & 0x7F);
                        if (index == 0) continue;
                        if (index > _properties.Count) throw Corrupt("property index out of range");
                        item.Properties.Add(_properties[index - 1]);
                    }
                }
            }
        }
    }

    private void ParseMoov(int start, int end)
    {
        // Only the first video track's first sample: trak/mdia/minf/stbl with stsd, stsz/stz2, stco/co64 and stsc.
        foreach (var (type, offset, length) in Boxes(start, end))
        {
            if (type != FourCc("trak") || FirstTrackSample is not null) continue;
            var stbl = Find(offset, offset + length, "mdia", "minf", "stbl");
            if (stbl is null) continue;
            var (stblOffset, stblLength) = stbl.Value;
            (int, int)? av1C = null;
            var size = (0, 0);
            long? firstSize = null;
            long? firstChunkOffset = null;
            foreach (var (t, o, l) in Boxes(stblOffset, stblOffset + stblLength))
            {
                var c = new Cursor(_data, o, o + l);
                if (t == FourCc("stsd"))
                {
                    c.U32();
                    c.U32(); // entry count
                    foreach (var (et, eo, el) in Boxes(c.Pos, o + l))
                    {
                        if (et != Av01 || el < 78) continue;
                        size = (BinaryPrimitives.ReadUInt16BigEndian(_data.Span[(eo + 24)..]), BinaryPrimitives.ReadUInt16BigEndian(_data.Span[(eo + 26)..]));
                        foreach (var (ct, co, cl) in Boxes(eo + 78, eo + el))
                            if (ct == Av1C) av1C = (co, cl);
                        break;
                    }
                }
                else if (t == FourCc("stsz"))
                {
                    c.U32();
                    var sampleSize = c.U32();
                    var count = c.U32();
                    if (count > 0) firstSize = sampleSize != 0 ? sampleSize : c.U32();
                }
                else if (t == FourCc("stz2"))
                {
                    c.U32();
                    var fieldSize = (int)(c.U32() & 0xFF);
                    var count = c.U32();
                    if (count > 0)
                        firstSize = fieldSize switch { 16 => c.U16(), 8 => c.U8(), 4 => c.U8() >> 4, _ => throw Corrupt("invalid stz2") };
                }
                else if (t == FourCc("stco"))
                {
                    c.U32();
                    if (c.U32() > 0) firstChunkOffset = c.U32();
                }
                else if (t == FourCc("co64"))
                {
                    c.U32();
                    if (c.U32() > 0) firstChunkOffset = (long)c.Read(8);
                }
            }
            // The first sample is always the first sample of the first chunk, whatever stsc says about the others.
            if (av1C is null || firstSize is null || firstChunkOffset is null) continue;
            if (firstChunkOffset < 0 || firstSize <= 0 || firstChunkOffset + firstSize > _fileLength) throw Corrupt("track sample outside the file");
            FirstTrackSample = ((int)firstChunkOffset.Value, (int)firstSize.Value);
            TrackAv1C = av1C;
            TrackSize = size;
        }
    }

    private (int Offset, int Length)? Find(int start, int end, params string[] path)
    {
        var range = (start, end - start);
        foreach (var name in path)
        {
            var found = false;
            foreach (var (type, offset, length) in Boxes(range.start, range.start + range.Item2))
            {
                if (type != FourCc(name)) continue;
                range = (offset, length);
                found = true;
                break;
            }
            if (!found) return null;
        }
        return range;
    }

    public HeifItem? GetItem(uint id) => Items.TryGetValue(id, out var item) ? item : null;

    public HeifProperty? FindProperty(HeifItem item, uint type)
    {
        foreach (var p in item.Properties)
            if (p.Type == type) return p;
        return null;
    }

    public ReadOnlySpan<byte> PropertyData(HeifProperty p) => _data.Span.Slice(p.Offset, p.Length);

    /// <summary>
    /// The bytes of an item, its extents concatenated after <paramref name="prefix"/> when one is given, checked against
    /// the file, and, the prefix included, against the size limit.
    /// </summary>
    public byte[] GetItemData(HeifItem item, long maxBytes, ReadOnlySpan<byte> prefix = default)
    {
        var (extents, total) = ResolveExtents(item, Math.Min(maxBytes, int.MaxValue) - prefix.Length);
        var result = new byte[prefix.Length + total];
        prefix.CopyTo(result);
        var pos = prefix.Length;
        foreach (var (start, length) in extents)
        {
            _data.Span.Slice(start, length).CopyTo(result.AsSpan(pos));
            pos += length;
        }
        return result;
    }

    /// <summary>The first extent of an item's data, read in place: where an AV1 item starts with its sequence header.</summary>
    public ReadOnlySpan<byte> GetItemDataStart(HeifItem item)
    {
        var (extents, _) = ResolveExtents(item, long.MaxValue);
        return _data.Span.Slice(extents[0].Start, extents[0].Length);
    }

    private ((int Start, int Length)[] Extents, int Total) ResolveExtents(HeifItem item, long maxBytes)
    {
        if (!item.HasLocation || item.Extents.Count == 0) throw Corrupt($"item {item.Id} has no data");
        int sourceStart, sourceLength;
        if (item.ConstructionMethod == 0)
        {
            sourceStart = 0;
            sourceLength = _data.Length;
        }
        else if (item.ConstructionMethod == 1)
        {
            if (_idatOffset < 0) throw Corrupt("idat item without an idat box");
            sourceStart = _idatOffset;
            sourceLength = _idatLength;
        }
        else
        {
            throw new ImageDecodeException("AVIF items constructed from other items are not supported.");
        }
        // An extent length of 0 means "to the end of the source"; resolve and check every extent before allocating.
        var resolved = new (int Start, int Length)[item.Extents.Count];
        long total = 0;
        for (var i = 0; i < item.Extents.Count; i++)
        {
            var (offset, length) = item.Extents[i];
            var start = item.BaseOffset + offset;
            if (start < item.BaseOffset || start > (ulong)sourceLength) throw Corrupt("item data outside the file");
            var len = length == 0 ? (ulong)sourceLength - start : length;
            if (len > (ulong)sourceLength - start) throw Corrupt("item data outside the file");
            resolved[i] = (sourceStart + (int)start, (int)len);
            total += (long)len;
            if (total > Math.Min(maxBytes, int.MaxValue)) throw new ImageLimitExceededException("The AVIF item is larger than the configured limit.");
        }
        return (resolved, (int)total);
    }
}
