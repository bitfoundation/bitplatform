using System.Buffers.Binary;
using System.Drawing;
using System.Text;

namespace Bit.Bmage;

/// <summary>
/// Managed AVIF decoder: the HEIF container (single images, grids, alpha, premultiplied alpha, clean aperture,
/// rotation and mirroring, the first frame of image sequences) and an AV1 decoder for intra frames covering every
/// coding tool, 8 to 12 bits and every chroma subsampling, with the loop filter, CDEF, loop restoration, superres and
/// film grain.
/// </summary>
public sealed class BmageAvifDecoder : IBmageImageDecoder
{
    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.Avif];
    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    private static readonly string[] AlphaUrns = ["urn:mpeg:mpegB:cicp:systems:auxiliary:alpha", "urn:mpeg:hevc:2015:auxid:1"];

    public BmageImageInfo? Identify(ReadOnlySpan<byte> data)
        => ReadSize(data, cropped: true) is { } size ? new BmageImageInfo(size.Width, size.Height, BmageImageFormat.Avif, BmageOrientation.Normal, size.HasAlpha) : null;

    /// <summary>
    /// The upright size a decoder allocates for the image: the whole coded image, before its clean aperture crops it to
    /// the size <see cref="Identify"/> reports.
    /// </summary>
    internal static (int Width, int Height)? UncroppedSize(ReadOnlySpan<byte> data)
        => ReadSize(data, cropped: false) is { } size ? (size.Width, size.Height) : null;

    /// <summary>
    /// How the primary image's irot and imir properties turn it upright, which is what a browser applies to an AVIF; an
    /// Exif orientation in the file does not change it. Normal when the file has none or cannot be read.
    /// </summary>
    internal static BmageOrientation ContainerOrientation(ReadOnlySpan<byte> data)
        => ReadSize(data, cropped: true) is { } size ? size.Orientation : BmageOrientation.Normal;

    private static (int Width, int Height, bool HasAlpha, BmageOrientation Orientation)? ReadSize(ReadOnlySpan<byte> data, bool cropped)
    {
        if (BmageImageFormats.Detect(data) != BmageImageFormat.Avif) return null;
        try
        {
            var file = BmageAvifContainer.ParseHeaders(data);
            var primary = PrimaryItem(file);
            if (primary is null)
            {
                if (file.TrackAv1C is null) return null;
                var (trackWidth, trackHeight) = file.TrackSize;
                return trackWidth > 0 && trackHeight > 0 ? (trackWidth, trackHeight, false, BmageOrientation.Normal) : null;
            }
            if (file.FindProperty(primary, BmageAvifContainer.Ispe) is not { Length: >= 12 } ispe) return null;
            var p = file.PropertyData(ispe);
            var width = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(p[4..]), int.MaxValue);
            var height = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(p[8..]), int.MaxValue);
            if (width <= 0 || height <= 0) return null;
            var (crop, orientation) = Transforms(file, primary, width, height);
            if (cropped) (width, height) = (crop.Width, crop.Height);
            if (orientation.SwapsDimensions()) (width, height) = (height, width);
            return (width, height, FindAlpha(file, primary) is not null, orientation);
        }
        catch (Exception ex) when (ex is BmageImageException or ArgumentException or IndexOutOfRangeException or OverflowException)
        {
            return null;
        }
    }

    public ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data, options, allocator, nearestChroma: false, cancellationToken));

    public static BmageImageBuffer Decode(ReadOnlySpan<byte> data, BmageDecodeOptions? options = null, IBmagePixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        // The container reads the file in place, which a span cannot be kept for, so it is copied, once it is known
        // not to be over the limit.
        (options ?? BmageDecodeOptions.Default).Limits.ValidateEncodedSize(data.Length);
        return Decode(data.ToArray(), options, allocator, nearestChroma: false, cancellationToken);
    }

    /// <summary>The decode, with chroma optionally upsampled by repetition (as libheif does) so tests can compare exactly.</summary>
    internal static BmageImageBuffer Decode(ReadOnlyMemory<byte> data, BmageDecodeOptions? options, IBmagePixelAllocator? allocator, bool nearestChroma, CancellationToken cancellationToken = default)
    {
        options ??= BmageDecodeOptions.Default;
        allocator ??= BmagePooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (BmageImageFormats.Detect(data.Span) != BmageImageFormat.Avif) throw new BmageImageDecodeException("Not an AVIF image.");
        try
        {
            return DecodeCore(data, options, allocator, nearestChroma, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or OverflowException)
        {
            // The parsers check every length they read, so this is a corrupt file reaching a case they did not foresee.
            throw new BmageImageDecodeException("Corrupt AVIF image data.", ex);
        }
    }

    private static BmageImageBuffer DecodeCore(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, bool nearestChroma, CancellationToken cancellationToken)
    {
        var file = BmageAvifContainer.Parse(data);
        var primary = PrimaryItem(file);
        BmageImageBuffer image;
        var orientation = BmageOrientation.Normal;
        if (primary is null)
        {
            if (file.FirstTrackSample is not { } sample || file.TrackAv1C is not { } config)
                throw new BmageImageDecodeException("The AVIF file has no primary image item and no image sequence.");
            using var frame = DecodeAv1(Concat(data.Span.Slice(config.Offset, config.Length), data.Span.Slice(sample.Offset, sample.Length)), options, cancellationToken);
            image = BmageImageBuffer.Create(frame.Width, frame.Height, allocator, clear: false, options.Limits);
            try
            {
                BmageAvifColor.WriteRgb(frame, ColorOf(file, null, frame.Seq), image, 0, 0, nearestChroma);
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
        else
        {
            if (file.FindProperty(primary, BmageAvifContainer.Ispe) is { Length: >= 12 } ispe)
            {
                var p = file.PropertyData(ispe);
                var w = BinaryPrimitives.ReadUInt32BigEndian(p[4..]);
                var h = BinaryPrimitives.ReadUInt32BigEndian(p[8..]);
                if (w == 0 || h == 0 || w > int.MaxValue || h > int.MaxValue) throw new BmageImageDecodeException("Invalid AVIF image size.");
                options.Limits.Validate((int)w, (int)h);
            }
            image = DecodeItem(file, primary, false, nearestChroma, options, allocator, cancellationToken);
            try
            {
                if (FindAlpha(file, primary) is { } alphaItem)
                {
                    DecodeAlpha(file, alphaItem, image, options, allocator, cancellationToken);
                    if (IsPremultiplied(primary, alphaItem)) BmageAvifColor.Unpremultiply(image);
                }
                (var crop, orientation) = Transforms(file, primary, image.Width, image.Height);
                if (crop.Width != image.Width || crop.Height != image.Height)
                {
                    var cropped = image.CopyRegion(crop, allocator);
                    image.Dispose();
                    image = cropped;
                }
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }

        try
        {
            BmageImageMetadata metadata;
            if (options.ReadMetadata)
            {
                try
                {
                    metadata = ReadMetadata(file, primary, options.FileName);
                }
                catch (Exception ex) when (ex is BmageImageException or ArgumentException or IndexOutOfRangeException)
                {
                    // Malformed metadata must never prevent an image from loading.
                    metadata = new BmageImageMetadata(sourceFormat: BmageImageFormat.Avif, fileName: options.FileName);
                }
            }
            else
            {
                metadata = new BmageImageMetadata(sourceFormat: BmageImageFormat.Avif, fileName: options.FileName);
            }
            if (options.AutoOrient)
            {
                // The HEIF transforms define the upright image; an Exif orientation is ignored for AVIF, so record the
                // result as upright.
                if (orientation != BmageOrientation.Normal)
                {
                    var context = new BmageOperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true, maxDegreeOfParallelism: options.MaxDegreeOfParallelism);
                    var oriented = new BmageOrientationOperation(orientation).Apply(image, context);
                    if (!ReferenceEquals(oriented, image)) image.Dispose();
                    image = oriented;
                }
                metadata = metadata.WithOrientation(BmageOrientation.Normal);
            }
            else
            {
                // Leave the pixels as stored and say, the EXIF way, how to show them.
                metadata = metadata.Exif is null
                    ? orientation == BmageOrientation.Normal ? metadata : new BmageImageMetadata(BmageExifData.FromOrientation(orientation), metadata.IccProfile, metadata.Xmp, BmageImageFormat.Avif, metadata.FileName)
                    : metadata.WithOrientation(orientation);
            }
            image.Metadata = metadata;
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    /// <summary>Extracts EXIF, ICC and XMP from an AVIF, read in place. Missing pieces are null or empty.</summary>
    internal static BmageImageMetadata ReadMetadata(ReadOnlyMemory<byte> avif, string? fileName = null)
    {
        var file = BmageAvifContainer.Parse(avif);
        return ReadMetadata(file, PrimaryItem(file), fileName);
    }

    private static BmageImageMetadata ReadMetadata(BmageAvifContainer file, BmageHeifItem? primary, string? fileName)
    {
        BmageExifData? exif = null;
        ReadOnlyMemory<byte> icc = default, xmp = default;
        if (primary is not null)
        {
            // A copy, so the metadata does not keep the whole file alive.
            icc = IccProfile(file, primary).ToArray();
            foreach (var item in file.Items.Values)
            {
                var describes = item.References.Any(r => r.Type == BmageAvifContainer.Cdsc && r.To == primary.Id);
                if (!describes) continue;
                if (item.Type == BmageAvifContainer.Exif && exif is null)
                {
                    var data = file.GetItemData(item, 16 * 1024 * 1024);
                    if (data.Length >= 4)
                    {
                        var offset = BinaryPrimitives.ReadUInt32BigEndian(data);
                        if (offset <= (uint)(data.Length - 4)) exif = BmageExifReader.Read(data.AsSpan(4 + (int)offset));
                    }
                }
                else if (item.Type == BmageAvifContainer.Mime && xmp.IsEmpty && item.ContentType == "application/rdf+xml")
                {
                    xmp = file.GetItemData(item, 16 * 1024 * 1024);
                }
            }
        }
        return new BmageImageMetadata(exif, icc, xmp, BmageImageFormat.Avif, fileName);
    }

    /// <inheritdoc cref="HasNonSrgbColors(ReadOnlyMemory{byte}, out ReadOnlyMemory{byte})"/>
    internal static bool HasNonSrgbColors(ReadOnlyMemory<byte> avif) => HasNonSrgbColors(avif, out _);

    /// <summary>
    /// True when the file says its colours are not sRGB's: wide gamut primaries such as Display P3 or BT.2020, or an HDR
    /// or linear transfer, in the description the decoder goes by: the nclx, or, where there is none or it leaves
    /// everything unspecified, the AV1 sequence header. This decoder does not convert them, so a host that can should let
    /// the browser decode these. The file is read in place, and its ICC profile, if any, is returned for the caller to
    /// judge, so one parse answers both questions.
    /// </summary>
    internal static bool HasNonSrgbColors(ReadOnlyMemory<byte> avif, out ReadOnlyMemory<byte> iccProfile)
    {
        var file = BmageAvifContainer.Parse(avif);
        var primary = PrimaryItem(file);
        iccProfile = default;
        BmageAvifNclx? color;
        if (primary is null)
        {
            color = file.TrackAv1C is { } config && BmageAv1Decoder.FindSequenceHeader(ConfigObus(avif.Span.Slice(config.Offset, config.Length))) is { } seq
                ? ColorOf(file, null, seq)
                : null;
        }
        else
        {
            if (IccProperty(file, primary) is { } icc) iccProfile = avif.Slice(icc.Offset + 4, icc.Length - 4);
            // As DecodeItem picks it: a grid's own colr, else its first tile's, and the sequence header of that tile.
            var coded = primary;
            if (primary.Type == BmageAvifContainer.Grid
                && primary.References.FirstOrDefault(r => r.Type == BmageAvifContainer.Dimg) is { To: var firstTile }
                && file.GetItem(firstTile) is { } tile)
                coded = tile;
            var colorItem = file.FindProperty(primary, BmageAvifContainer.Colr) is not null ? primary : coded;
            color = ItemColor(file, colorItem) ?? (SequenceHeaderOf(file, coded) is { } seq ? ColorOf(file, null, seq) : null);
        }
        // BT.709 (sRGB's), unspecified, and BT.601's two, which differ from them too little to see; then linear, PQ and HLG.
        return color is { } c && (c.ColorPrimaries is not (1 or 2 or 5 or 6) || c.TransferCharacteristics is 8 or 16 or 18);
    }

    /// <summary>An av01 item's sequence header: av1C's copy, else the one the item's data starts with.</summary>
    private static BmageAv1SequenceHeader? SequenceHeaderOf(BmageAvifContainer file, BmageHeifItem item)
    {
        if (item.Type != BmageAvifContainer.Av01) return null;
        if (file.FindProperty(item, BmageAvifContainer.Av1C) is { } config && BmageAv1Decoder.FindSequenceHeader(ConfigObus(file.PropertyData(config))) is { } seq)
            return seq;
        return BmageAv1Decoder.FindSequenceHeader(file.GetItemDataStart(item));
    }

    private static BmageHeifProperty? IccProperty(BmageAvifContainer file, BmageHeifItem item)
    {
        foreach (var property in item.Properties)
        {
            if (property.Type != BmageAvifContainer.Colr || property.Length < 4) continue;
            var colourType = BinaryPrimitives.ReadUInt32BigEndian(file.PropertyData(property));
            if (colourType == BmageAvifContainer.Prof || colourType == BmageAvifContainer.Ricc) return property;
        }
        return null;
    }

    private static ReadOnlySpan<byte> IccProfile(BmageAvifContainer file, BmageHeifItem item)
        => IccProperty(file, item) is { } icc ? file.PropertyData(icc)[4..] : default;

    private static BmageHeifItem? PrimaryItem(BmageAvifContainer file)
    {
        if (!file.HasMeta || file.PrimaryItemId == 0) return null;
        var item = file.GetItem(file.PrimaryItemId);
        if (item is null) throw new BmageImageDecodeException("Invalid AVIF file: the primary item does not exist.");
        return item;
    }

    private static BmageHeifItem? FindAlpha(BmageAvifContainer file, BmageHeifItem primary)
    {
        foreach (var item in file.Items.Values)
        {
            if (!item.References.Any(r => r.Type == BmageAvifContainer.Auxl && r.To == primary.Id)) continue;
            if (file.FindProperty(item, BmageAvifContainer.AuxC) is not { Length: >= 4 } auxC) continue;
            var p = file.PropertyData(auxC)[4..];
            var nul = p.IndexOf((byte)0);
            var urn = Encoding.ASCII.GetString(nul >= 0 ? p[..nul] : p);
            if (Array.IndexOf(AlphaUrns, urn) >= 0) return item;
        }
        return null;
    }

    private static bool IsPremultiplied(BmageHeifItem primary, BmageHeifItem alpha)
        => primary.References.Any(r => r.Type == BmageAvifContainer.Prem && r.To == alpha.Id)
           || alpha.References.Any(r => r.Type == BmageAvifContainer.Prem && r.To == primary.Id);

    /// <summary>Decodes an av01 or grid item to RGBA (with alpha 255), or with <paramref name="alphaOnly"/>, into the alpha of <paramref name="target"/>.</summary>
    private static BmageImageBuffer DecodeItem(BmageAvifContainer file, BmageHeifItem item, bool alphaOnly, bool nearestChroma, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken, BmageImageBuffer? target = null)
    {
        if (item.Type == BmageAvifContainer.Av01)
        {
            using var frame = DecodeAv1Item(file, item, options, cancellationToken);
            if (alphaOnly)
            {
                if (frame.Width != target!.Width || frame.Height != target.Height) throw new BmageImageDecodeException("The AVIF alpha image does not match the colour image's size.");
                BmageAvifColor.WriteAlpha(frame, target, 0, 0);
                return target;
            }
            var image = BmageImageBuffer.Create(frame.Width, frame.Height, allocator, clear: false, options.Limits);
            try
            {
                BmageAvifColor.WriteRgb(frame, ColorOf(file, item, frame.Seq), image, 0, 0, nearestChroma);
                return image;
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
        if (item.Type != BmageAvifContainer.Grid) throw new BmageImageDecodeException($"AVIF items of type '{FourCcName(item.Type)}' are not supported.");

        var grid = file.GetItemData(item, 64);
        if (grid.Length < 8 || grid[0] != 0) throw new BmageImageDecodeException("Invalid AVIF image grid.");
        var rows = grid[2] + 1;
        var cols = grid[3] + 1;
        int outW, outH;
        if ((grid[1] & 1) != 0)
        {
            if (grid.Length < 12) throw new BmageImageDecodeException("Invalid AVIF image grid.");
            var w = BinaryPrimitives.ReadUInt32BigEndian(grid.AsSpan(4));
            var h = BinaryPrimitives.ReadUInt32BigEndian(grid.AsSpan(8));
            if (w == 0 || h == 0 || w > int.MaxValue || h > int.MaxValue) throw new BmageImageDecodeException("Invalid AVIF image grid size.");
            outW = (int)w;
            outH = (int)h;
        }
        else
        {
            outW = BinaryPrimitives.ReadUInt16BigEndian(grid.AsSpan(4));
            outH = BinaryPrimitives.ReadUInt16BigEndian(grid.AsSpan(6));
        }
        options.Limits.Validate(outW, outH);
        var tiles = item.References.Where(r => r.Type == BmageAvifContainer.Dimg).Select(r => r.To).ToArray();
        if (tiles.Length != rows * cols) throw new BmageImageDecodeException("The AVIF image grid lists the wrong number of tiles.");
        var tileItems = new BmageHeifItem[tiles.Length];
        for (var i = 0; i < tiles.Length; i++)
        {
            tileItems[i] = file.GetItem(tiles[i]) ?? throw new BmageImageDecodeException("An AVIF grid tile does not exist.");
            if (tileItems[i].Type != BmageAvifContainer.Av01) throw new BmageImageDecodeException("AVIF grid tiles must be AV1 images.");
        }
        // A grid may name one item for many cells. Each item is decoded once, by one thread, and written to all of its cells
        // before it is released, so a small file cannot demand decoding out of proportion to its size.
        var cellsOf = new Dictionary<uint, List<int>>();
        var groups = new List<List<int>>();
        for (var i = 0; i < tiles.Length; i++)
        {
            if (!cellsOf.TryGetValue(tiles[i], out var cells)) groups.Add(cellsOf[tiles[i]] = cells = []);
            cells.Add(i);
        }
        if (alphaOnly && (target!.Width != outW || target.Height != outH)) throw new BmageImageDecodeException("The AVIF alpha image does not match the colour image's size.");
        var result = alphaOnly ? target! : BmageImageBuffer.Create(outW, outH, allocator, clear: true, options.Limits);
        try
        {
            var tileW = 0;
            var tileH = 0;
            var color = default(BmageAvifNclx);
            var parallelism = Math.Max(1, options.MaxDegreeOfParallelism ?? BmageOperationContext.DefaultMaxDegreeOfParallelism);
            void WriteCells(BmageAv1Image frame, List<int> cells)
            {
                foreach (var i in cells)
                {
                    var x = i % cols * tileW;
                    var y = i / cols * tileH;
                    if (alphaOnly) BmageAvifColor.WriteAlpha(frame, result, x, y);
                    else BmageAvifColor.WriteRgb(frame, color, result, x, y, nearestChroma);
                }
            }
            void DecodeTile(int g)
            {
                var cells = groups[g];
                using var frame = DecodeAv1Item(file, tileItems[cells[0]], options, cancellationToken);
                if (frame.Width != tileW || frame.Height != tileH) throw new BmageImageDecodeException("AVIF grid tiles differ in size.");
                WriteCells(frame, cells);
            }
            // The first tile sets the tile size and colour; the others can then decode side by side.
            using (var first = DecodeAv1Item(file, tileItems[0], options, cancellationToken))
            {
                tileW = first.Width;
                tileH = first.Height;
                // HEIF has the tiles cover the output exactly, the last row and column overhanging it by less than a tile,
                // which also bounds the decoding work to about the output's size.
                if ((long)tileW * cols < outW || (long)tileH * rows < outH || (long)tileW * (cols - 1) >= outW || (long)tileH * (rows - 1) >= outH)
                    throw new BmageImageDecodeException("The AVIF image grid tiles do not match the image size.");
                color = ColorOf(file, file.FindProperty(item, BmageAvifContainer.Colr) is not null ? item : tileItems[0], first.Seq);
                WriteCells(first, groups[0]);
            }
            if (parallelism > 1 && groups.Count > 2)
                BmageParallelRows.For(1, groups.Count, new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cancellationToken }, DecodeTile);
            else
                for (var g = 1; g < groups.Count; g++) DecodeTile(g);
            return result;
        }
        catch
        {
            if (!alphaOnly) result.Dispose();
            throw;
        }
    }

    private static void DecodeAlpha(BmageAvifContainer file, BmageHeifItem alpha, BmageImageBuffer image, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken)
        => DecodeItem(file, alpha, true, false, options, allocator, cancellationToken, image);

    private static BmageAv1Image DecodeAv1Item(BmageAvifContainer file, BmageHeifItem item, BmageDecodeOptions options, CancellationToken cancellationToken)
    {
        // The sequence header normally travels in the item too, but av1C's copy comes first in case it does not.
        var config = file.FindProperty(item, BmageAvifContainer.Av1C) is { } c ? ConfigObus(file.PropertyData(c)) : default;
        var obus = file.GetItemData(item, options.Limits.MaxEncodedBytes, config);
        var frame = DecodeAv1(obus, options, cancellationToken);
        // A layered image whose enhancement layers predict from the base layer decodes to its base layer only, which is
        // acceptable when that is the full picture (a quality layer) and not when it is a smaller preview.
        if (frame.SkippedFrames && file.FindProperty(item, BmageAvifContainer.Ispe) is { Length: >= 12 } ispe)
        {
            var p = file.PropertyData(ispe);
            if (BinaryPrimitives.ReadUInt32BigEndian(p[4..]) != frame.Width || BinaryPrimitives.ReadUInt32BigEndian(p[8..]) != frame.Height)
            {
                frame.Dispose();
                throw new BmageImageDecodeException("This layered (progressive) AVIF image builds its full size from inter predicted layers, which the managed decoder does not support.");
            }
        }
        return frame;
    }

    private static BmageAv1Image DecodeAv1(byte[] obus, BmageDecodeOptions options, CancellationToken cancellationToken)
        => BmageAv1Decoder.Decode(obus, 0, obus.Length, options.Limits, cancellationToken);

    /// <summary>The av1C configOBUs, after its four header bytes.</summary>
    private static ReadOnlySpan<byte> ConfigObus(ReadOnlySpan<byte> av1C) => av1C.Length > 4 ? av1C[4..] : default;

    /// <summary>The av1C configOBUs followed by the sample's OBUs.</summary>
    private static byte[] Concat(ReadOnlySpan<byte> av1C, ReadOnlySpan<byte> sample)
    {
        var configObus = ConfigObus(av1C);
        var result = new byte[configObus.Length + sample.Length];
        configObus.CopyTo(result);
        sample.CopyTo(result.AsSpan(configObus.Length));
        return result;
    }

    /// <summary>The item's nclx colour, range included, else the AV1 sequence header's.</summary>
    private static BmageAvifNclx ColorOf(BmageAvifContainer file, BmageHeifItem? item, BmageAv1SequenceHeader seq)
        => (item is null ? null : ItemColor(file, item))
           ?? new BmageAvifNclx(seq.ColorPrimaries, seq.TransferCharacteristics, seq.MatrixCoefficients, seq.FullRange);

    /// <summary>The item's nclx colour, range included, or null when it has none that says anything.</summary>
    private static BmageAvifNclx? ItemColor(BmageAvifContainer file, BmageHeifItem item)
    {
        foreach (var property in item.Properties)
        {
            if (property.Type != BmageAvifContainer.Colr || property.Length < 11) continue;
            var p = file.PropertyData(property);
            if (BinaryPrimitives.ReadUInt32BigEndian(p) != BmageAvifContainer.Nclx) continue;
            var nclx = new BmageAvifNclx(BinaryPrimitives.ReadUInt16BigEndian(p[4..]), BinaryPrimitives.ReadUInt16BigEndian(p[6..]),
                BinaryPrimitives.ReadUInt16BigEndian(p[8..]), (p[10] & 0x80) != 0);
            // An nclx that leaves primaries, transfer and matrix unspecified says nothing reliable, its range flag
            // included (early writers set it regardless), so the bitstream's description is used, as libheif does.
            return nclx is { ColorPrimaries: 2, TransferCharacteristics: 2, MatrixCoefficients: 2 } ? null : nclx;
        }
        return null;
    }

    /// <summary>
    /// The part of the decoded image an item keeps, and how to turn it upright, from its clap, irot and imir properties
    /// in the order listed. Decoding and identifying both go by this, so they agree on the size.
    /// </summary>
    private static (Rectangle Crop, BmageOrientation Orientation) Transforms(BmageAvifContainer file, BmageHeifItem item, int width, int height)
    {
        var crop = new Rectangle(0, 0, width, height);
        var orientation = BmageOrientation.Normal;
        foreach (var property in item.Properties)
        {
            if (property.Type == BmageAvifContainer.Clap)
            {
                // MIAF puts clap before irot and imir; one listed after them is ignored.
                if (orientation == BmageOrientation.Normal && CleanAperture(file.PropertyData(property), crop.Width, crop.Height) is { } c)
                    crop = new Rectangle(crop.X + c.X, crop.Y + c.Y, c.Width, c.Height);
            }
            else if (property.Type == BmageAvifContainer.Irot && property.Length >= 1)
            {
                // irot turns anti-clockwise by multiples of 90 degrees.
                orientation = orientation.Then((file.PropertyData(property)[0] & 3) switch
                {
                    1 => BmageOrientation.Rotate270,
                    2 => BmageOrientation.Rotate180,
                    3 => BmageOrientation.Rotate90,
                    _ => BmageOrientation.Normal,
                });
            }
            else if (property.Type == BmageAvifContainer.Imir && property.Length >= 1)
            {
                // Axis (named mode in the 2022 edition of HEIF) 0 exchanges top and bottom, 1 left and right.
                orientation = orientation.Then((file.PropertyData(property)[0] & 1) == 0 ? BmageOrientation.FlipVertical : BmageOrientation.FlipHorizontal);
            }
        }
        return (crop, orientation);
    }

    /// <summary>The rectangle a clap property keeps, or null when it is invalid or reaches outside the image.</summary>
    private static Rectangle? CleanAperture(ReadOnlySpan<byte> p, int width, int height)
    {
        if (p.Length < 32) return null;
        double widthN = BinaryPrimitives.ReadUInt32BigEndian(p), widthD = BinaryPrimitives.ReadUInt32BigEndian(p[4..]);
        double heightN = BinaryPrimitives.ReadUInt32BigEndian(p[8..]), heightD = BinaryPrimitives.ReadUInt32BigEndian(p[12..]);
        double horizN = BinaryPrimitives.ReadInt32BigEndian(p[16..]), horizD = BinaryPrimitives.ReadUInt32BigEndian(p[20..]);
        double vertN = BinaryPrimitives.ReadInt32BigEndian(p[24..]), vertD = BinaryPrimitives.ReadUInt32BigEndian(p[28..]);
        if (widthD == 0 || heightD == 0 || horizD == 0 || vertD == 0) return null;
        // The aperture is centred on the image centre plus the offset. The bounds are checked before anything becomes an
        // int, since a crafted offset puts the edges far outside int's range.
        var w = Math.Round(widthN / widthD);
        var h = Math.Round(heightN / heightD);
        var x = Math.Round(horizN / horizD + (width - widthN / widthD) / 2);
        var y = Math.Round(vertN / vertD + (height - heightN / heightD) / 2);
        if (w <= 0 || h <= 0 || x < 0 || y < 0 || x + w > width || y + h > height) return null;
        return new Rectangle((int)x, (int)y, (int)w, (int)h);
    }

    private static string FourCcName(uint type) => new([(char)(type >> 24), (char)((type >> 16) & 0xFF), (char)((type >> 8) & 0xFF), (char)(type & 0xFF)]);
}
