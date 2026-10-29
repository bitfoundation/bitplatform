using System.Buffers.Binary;
using System.Drawing;
using System.Text;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Codecs.Avif;

/// <summary>
/// Managed AVIF decoder: the HEIF container (single images, grids, alpha, premultiplied alpha, clean aperture,
/// rotation and mirroring, the first frame of image sequences) and an AV1 decoder for intra frames covering every
/// coding tool, 8 to 12 bits and every chroma subsampling, with the loop filter, CDEF, loop restoration, superres and
/// film grain.
/// </summary>
public sealed class AvifDecoder : IImageDecoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Avif];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    private static readonly string[] AlphaUrns = ["urn:mpeg:mpegB:cicp:systems:auxiliary:alpha", "urn:mpeg:hevc:2015:auxid:1"];

    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (ImageFormats.Detect(data) != ImageFormat.Avif) return null;
        try
        {
            var file = AvifContainer.Parse(data.ToArray());
            var primary = PrimaryItem(file);
            if (primary is null)
            {
                if (file.TrackAv1C is null) return null;
                var (trackWidth, trackHeight) = file.TrackSize;
                return trackWidth > 0 && trackHeight > 0 ? new ImageInfo(trackWidth, trackHeight, ImageFormat.Avif, Orientation.Normal, false) : null;
            }
            if (file.FindProperty(primary, AvifContainer.Ispe) is not { Length: >= 12 } ispe) return null;
            var p = file.PropertyData(ispe);
            var width = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(p[4..]), int.MaxValue);
            var height = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(p[8..]), int.MaxValue);
            if (width <= 0 || height <= 0) return null;
            var transposed = false;
            foreach (var property in primary.Properties)
            {
                if (property.Type == AvifContainer.Clap && CleanAperture(file.PropertyData(property), width, height) is { } crop && !transposed)
                {
                    width = crop.Width;
                    height = crop.Height;
                }
                else if (property.Type == AvifContainer.Irot && property.Length >= 1 && (file.PropertyData(property)[0] & 1) == 1)
                {
                    (width, height) = (height, width);
                    transposed = !transposed;
                }
            }
            return new ImageInfo(width, height, ImageFormat.Avif, Orientation.Normal, FindAlpha(file, primary) is not null);
        }
        catch (Exception ex) when (ex is ImageException or ArgumentException or IndexOutOfRangeException or OverflowException)
        {
            return null;
        }
    }

    public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options = null, IPixelAllocator? allocator = null, CancellationToken cancellationToken = default)
        => Decode(data, options, allocator, nearestChroma: false, cancellationToken);

    /// <summary>The decode, with chroma optionally upsampled by repetition (as libheif does) so tests can compare exactly.</summary>
    internal static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options, IPixelAllocator? allocator, bool nearestChroma, CancellationToken cancellationToken = default)
    {
        options ??= DecodeOptions.Default;
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (ImageFormats.Detect(data) != ImageFormat.Avif) throw new ImageDecodeException("Not an AVIF image.");
        try
        {
            return DecodeCore(data.ToArray(), options, allocator, nearestChroma, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or OverflowException)
        {
            // The parsers check every length they read, so this is a corrupt file reaching a case they did not foresee.
            throw new ImageDecodeException("Corrupt AVIF image data.", ex);
        }
    }

    private static ImageBuffer DecodeCore(byte[] bytes, DecodeOptions options, IPixelAllocator allocator, bool nearestChroma, CancellationToken cancellationToken)
    {
        var file = AvifContainer.Parse(bytes);
        var primary = PrimaryItem(file);
        ImageBuffer image;
        var orientation = Orientation.Normal;
        if (primary is null)
        {
            if (file.FirstTrackSample is not { } sample || file.TrackAv1C is not { } config)
                throw new ImageDecodeException("The AVIF file has no primary image item and no image sequence.");
            using var frame = DecodeAv1(Concat(file.Data.AsSpan(config.Offset, config.Length), file.Data.AsSpan(sample.Offset, sample.Length)), options, cancellationToken);
            image = ImageBuffer.Create(frame.Width, frame.Height, allocator, clear: false, options.Limits);
            try
            {
                AvifColor.WriteRgb(frame, ColorOf(file, null, frame.Seq), image, 0, 0, nearestChroma);
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
        else
        {
            if (file.FindProperty(primary, AvifContainer.Ispe) is { Length: >= 12 } ispe)
            {
                var p = file.PropertyData(ispe);
                var w = BinaryPrimitives.ReadUInt32BigEndian(p[4..]);
                var h = BinaryPrimitives.ReadUInt32BigEndian(p[8..]);
                if (w == 0 || h == 0 || w > int.MaxValue || h > int.MaxValue) throw new ImageDecodeException("Invalid AVIF image size.");
                options.Limits.Validate((int)w, (int)h);
            }
            image = DecodeItem(file, primary, false, nearestChroma, options, allocator, cancellationToken);
            try
            {
                if (FindAlpha(file, primary) is { } alphaItem)
                {
                    DecodeAlpha(file, alphaItem, image, options, allocator, cancellationToken);
                    if (IsPremultiplied(primary, alphaItem)) AvifColor.Unpremultiply(image);
                }
                foreach (var property in primary.Properties)
                {
                    if (property.Type == AvifContainer.Clap)
                    {
                        // MIAF puts clap before irot and imir; one listed after them is ignored.
                        if (orientation != Orientation.Normal) continue;
                        if (CleanAperture(file.PropertyData(property), image.Width, image.Height) is { } crop
                            && (crop.Width != image.Width || crop.Height != image.Height))
                        {
                            var cropped = image.CopyRegion(crop, allocator);
                            image.Dispose();
                            image = cropped;
                        }
                    }
                    else if (property.Type == AvifContainer.Irot && property.Length >= 1)
                    {
                        // irot turns anti-clockwise by multiples of 90 degrees.
                        orientation = orientation.Then((file.PropertyData(property)[0] & 3) switch
                        {
                            1 => Orientation.Rotate270,
                            2 => Orientation.Rotate180,
                            3 => Orientation.Rotate90,
                            _ => Orientation.Normal,
                        });
                    }
                    else if (property.Type == AvifContainer.Imir && property.Length >= 1)
                    {
                        // Axis (named mode in the 2022 edition of HEIF) 0 exchanges top and bottom, 1 left and right.
                        orientation = orientation.Then((file.PropertyData(property)[0] & 1) == 0 ? Orientation.FlipVertical : Orientation.FlipHorizontal);
                    }
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
            ImageMetadata metadata;
            if (options.ReadMetadata)
            {
                try
                {
                    metadata = ReadMetadata(file, primary, options.FileName);
                }
                catch (Exception ex) when (ex is ImageException or ArgumentException or IndexOutOfRangeException)
                {
                    // Malformed metadata must never prevent an image from loading.
                    metadata = new ImageMetadata(sourceFormat: ImageFormat.Avif, fileName: options.FileName);
                }
            }
            else
            {
                metadata = new ImageMetadata(sourceFormat: ImageFormat.Avif, fileName: options.FileName);
            }
            if (options.AutoOrient)
            {
                // The HEIF transforms define the upright image; an Exif orientation is ignored for AVIF, so record the
                // result as upright.
                if (orientation != Orientation.Normal)
                {
                    var context = new OperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true, maxDegreeOfParallelism: options.MaxDegreeOfParallelism);
                    var oriented = new OrientationOperation(orientation).Apply(image, context);
                    if (!ReferenceEquals(oriented, image)) image.Dispose();
                    image = oriented;
                }
                metadata = metadata.WithOrientation(Orientation.Normal);
            }
            else
            {
                // Leave the pixels as stored and say, the EXIF way, how to show them.
                metadata = metadata.Exif is null
                    ? orientation == Orientation.Normal ? metadata : new ImageMetadata(ExifData.FromOrientation(orientation), metadata.IccProfile, metadata.Xmp, ImageFormat.Avif, metadata.FileName)
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

    /// <summary>Extracts EXIF, ICC and XMP from an AVIF. Missing pieces are null or empty.</summary>
    internal static ImageMetadata ReadMetadata(ReadOnlySpan<byte> avif, string? fileName = null)
    {
        var file = AvifContainer.Parse(avif.ToArray());
        return ReadMetadata(file, PrimaryItem(file), fileName);
    }

    private static ImageMetadata ReadMetadata(AvifContainer file, HeifItem? primary, string? fileName)
    {
        ExifData? exif = null;
        ReadOnlyMemory<byte> icc = default, xmp = default;
        if (primary is not null)
        {
            foreach (var property in primary.Properties)
            {
                if (property.Type != AvifContainer.Colr || property.Length < 4) continue;
                var p = file.PropertyData(property);
                var colourType = BinaryPrimitives.ReadUInt32BigEndian(p);
                if (colourType == AvifContainer.Prof || colourType == AvifContainer.Ricc)
                {
                    icc = p[4..].ToArray();
                    break;
                }
            }
            foreach (var item in file.Items.Values)
            {
                var describes = item.References.Any(r => r.Type == AvifContainer.Cdsc && r.To == primary.Id);
                if (!describes) continue;
                if (item.Type == AvifContainer.Exif && exif is null)
                {
                    var data = file.GetItemData(item, 16 * 1024 * 1024);
                    if (data.Length >= 4)
                    {
                        var offset = BinaryPrimitives.ReadUInt32BigEndian(data);
                        if (offset <= (uint)(data.Length - 4)) exif = ExifReader.Read(data.AsSpan(4 + (int)offset));
                    }
                }
                else if (item.Type == AvifContainer.Mime && xmp.IsEmpty && item.ContentType == "application/rdf+xml")
                {
                    xmp = file.GetItemData(item, 16 * 1024 * 1024);
                }
            }
        }
        return new ImageMetadata(exif, icc, xmp, ImageFormat.Avif, fileName);
    }

    /// <summary>
    /// True when the file says its colours are not sRGB's: wide gamut primaries such as Display P3 or BT.2020, or an HDR
    /// or linear transfer. This decoder does not convert them, so a host that can should let the browser decode these.
    /// </summary>
    internal static bool HasNonSrgbColors(ReadOnlySpan<byte> avif)
    {
        var file = AvifContainer.Parse(avif.ToArray());
        if (PrimaryItem(file) is not { } item) return false;
        foreach (var property in item.Properties)
        {
            if (property.Type != AvifContainer.Colr || property.Length < 11) continue;
            var p = file.PropertyData(property);
            if (BinaryPrimitives.ReadUInt32BigEndian(p) != AvifContainer.Nclx) continue;
            int primaries = BinaryPrimitives.ReadUInt16BigEndian(p[4..]);
            int transfer = BinaryPrimitives.ReadUInt16BigEndian(p[6..]);
            // BT.709 (sRGB's), unspecified, and BT.601's two, which differ from them too little to see.
            if (primaries is not (1 or 2 or 5 or 6)) return true;
            // Linear, PQ and HLG.
            if (transfer is 8 or 16 or 18) return true;
        }
        return false;
    }

    private static HeifItem? PrimaryItem(AvifContainer file)
    {
        if (!file.HasMeta || file.PrimaryItemId == 0) return null;
        var item = file.GetItem(file.PrimaryItemId);
        if (item is null) throw new ImageDecodeException("Invalid AVIF file: the primary item does not exist.");
        return item;
    }

    private static HeifItem? FindAlpha(AvifContainer file, HeifItem primary)
    {
        foreach (var item in file.Items.Values)
        {
            if (!item.References.Any(r => r.Type == AvifContainer.Auxl && r.To == primary.Id)) continue;
            if (file.FindProperty(item, AvifContainer.AuxC) is not { Length: >= 4 } auxC) continue;
            var p = file.PropertyData(auxC)[4..];
            var nul = p.IndexOf((byte)0);
            var urn = Encoding.ASCII.GetString(nul >= 0 ? p[..nul] : p);
            if (Array.IndexOf(AlphaUrns, urn) >= 0) return item;
        }
        return null;
    }

    private static bool IsPremultiplied(HeifItem primary, HeifItem alpha)
        => primary.References.Any(r => r.Type == AvifContainer.Prem && r.To == alpha.Id)
           || alpha.References.Any(r => r.Type == AvifContainer.Prem && r.To == primary.Id);

    /// <summary>Decodes an av01 or grid item to RGBA (with alpha 255), or with <paramref name="alphaOnly"/>, into the alpha of <paramref name="target"/>.</summary>
    private static ImageBuffer DecodeItem(AvifContainer file, HeifItem item, bool alphaOnly, bool nearestChroma, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken, ImageBuffer? target = null)
    {
        if (item.Type == AvifContainer.Av01)
        {
            using var frame = DecodeAv1Item(file, item, options, cancellationToken);
            if (alphaOnly)
            {
                if (frame.Width != target!.Width || frame.Height != target.Height) throw new ImageDecodeException("The AVIF alpha image does not match the colour image's size.");
                AvifColor.WriteAlpha(frame, target, 0, 0);
                return target;
            }
            var image = ImageBuffer.Create(frame.Width, frame.Height, allocator, clear: false, options.Limits);
            try
            {
                AvifColor.WriteRgb(frame, ColorOf(file, item, frame.Seq), image, 0, 0, nearestChroma);
                return image;
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
        if (item.Type != AvifContainer.Grid) throw new ImageDecodeException($"AVIF items of type '{FourCcName(item.Type)}' are not supported.");

        var grid = file.GetItemData(item, 64);
        if (grid.Length < 8 || grid[0] != 0) throw new ImageDecodeException("Invalid AVIF image grid.");
        var rows = grid[2] + 1;
        var cols = grid[3] + 1;
        int outW, outH;
        if ((grid[1] & 1) != 0)
        {
            if (grid.Length < 12) throw new ImageDecodeException("Invalid AVIF image grid.");
            var w = BinaryPrimitives.ReadUInt32BigEndian(grid.AsSpan(4));
            var h = BinaryPrimitives.ReadUInt32BigEndian(grid.AsSpan(8));
            if (w == 0 || h == 0 || w > int.MaxValue || h > int.MaxValue) throw new ImageDecodeException("Invalid AVIF image grid size.");
            outW = (int)w;
            outH = (int)h;
        }
        else
        {
            outW = BinaryPrimitives.ReadUInt16BigEndian(grid.AsSpan(4));
            outH = BinaryPrimitives.ReadUInt16BigEndian(grid.AsSpan(6));
        }
        options.Limits.Validate(outW, outH);
        var tiles = item.References.Where(r => r.Type == AvifContainer.Dimg).Select(r => r.To).ToArray();
        if (tiles.Length != rows * cols) throw new ImageDecodeException("The AVIF image grid lists the wrong number of tiles.");
        var tileItems = new HeifItem[tiles.Length];
        for (var i = 0; i < tiles.Length; i++)
        {
            tileItems[i] = file.GetItem(tiles[i]) ?? throw new ImageDecodeException("An AVIF grid tile does not exist.");
            if (tileItems[i].Type != AvifContainer.Av01) throw new ImageDecodeException("AVIF grid tiles must be AV1 images.");
        }
        if (alphaOnly && (target!.Width != outW || target.Height != outH)) throw new ImageDecodeException("The AVIF alpha image does not match the colour image's size.");
        var result = alphaOnly ? target! : ImageBuffer.Create(outW, outH, allocator, clear: true, options.Limits);
        try
        {
            var tileW = 0;
            var tileH = 0;
            var color = default(AvifNclx);
            var parallelism = Math.Max(1, options.MaxDegreeOfParallelism ?? OperationContext.DefaultMaxDegreeOfParallelism);
            void DecodeTile(int i)
            {
                using var frame = DecodeAv1Item(file, tileItems[i], options, cancellationToken);
                if (frame.Width != tileW || frame.Height != tileH) throw new ImageDecodeException("AVIF grid tiles differ in size.");
                var x = i % cols * tileW;
                var y = i / cols * tileH;
                if (alphaOnly) AvifColor.WriteAlpha(frame, result, x, y);
                else AvifColor.WriteRgb(frame, color, result, x, y, nearestChroma);
            }
            // The first tile sets the tile size and colour; the others can then decode side by side.
            using (var first = DecodeAv1Item(file, tileItems[0], options, cancellationToken))
            {
                tileW = first.Width;
                tileH = first.Height;
                // HEIF has the tiles cover the output exactly, the last row and column overhanging it by less than a tile,
                // which also bounds the decoding work to about the output's size.
                if ((long)tileW * cols < outW || (long)tileH * rows < outH || (long)tileW * (cols - 1) >= outW || (long)tileH * (rows - 1) >= outH)
                    throw new ImageDecodeException("The AVIF image grid tiles do not match the image size.");
                color = ColorOf(file, file.FindProperty(item, AvifContainer.Colr) is not null ? item : tileItems[0], first.Seq);
                if (alphaOnly) AvifColor.WriteAlpha(first, result, 0, 0);
                else AvifColor.WriteRgb(first, color, result, 0, 0, nearestChroma);
            }
            if (parallelism > 1 && tiles.Length > 2)
                Parallel.For(1, tiles.Length, new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cancellationToken }, DecodeTile);
            else
                for (var i = 1; i < tiles.Length; i++) DecodeTile(i);
            return result;
        }
        catch
        {
            if (!alphaOnly) result.Dispose();
            throw;
        }
    }

    private static void DecodeAlpha(AvifContainer file, HeifItem alpha, ImageBuffer image, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken)
        => DecodeItem(file, alpha, true, false, options, allocator, cancellationToken, image);

    private static Av1Image DecodeAv1Item(AvifContainer file, HeifItem item, DecodeOptions options, CancellationToken cancellationToken)
    {
        var data = file.GetItemData(item, options.Limits.MaxEncodedBytes);
        // The sequence header normally travels in the item too, but av1C's copy comes first in case it does not.
        var config = file.FindProperty(item, AvifContainer.Av1C);
        var obus = config is { Length: > 4 } c ? Concat(file.PropertyData(c), data) : data;
        var frame = DecodeAv1(obus, options, cancellationToken);
        // A layered image whose enhancement layers predict from the base layer decodes to its base layer only, which is
        // acceptable when that is the full picture (a quality layer) and not when it is a smaller preview.
        if (frame.SkippedFrames && file.FindProperty(item, AvifContainer.Ispe) is { Length: >= 12 } ispe)
        {
            var p = file.PropertyData(ispe);
            if (BinaryPrimitives.ReadUInt32BigEndian(p[4..]) != frame.Width || BinaryPrimitives.ReadUInt32BigEndian(p[8..]) != frame.Height)
            {
                frame.Dispose();
                throw new ImageDecodeException("This layered (progressive) AVIF image builds its full size from inter predicted layers, which the managed decoder does not support.");
            }
        }
        return frame;
    }

    private static Av1Image DecodeAv1(byte[] obus, DecodeOptions options, CancellationToken cancellationToken)
        => Av1Decoder.Decode(obus, 0, obus.Length, options.Limits, cancellationToken);

    /// <summary>The av1C configOBUs (after its four header bytes) followed by the sample's OBUs.</summary>
    private static byte[] Concat(ReadOnlySpan<byte> av1C, ReadOnlySpan<byte> sample)
    {
        var configObus = av1C.Length > 4 ? av1C[4..] : default;
        var result = new byte[configObus.Length + sample.Length];
        configObus.CopyTo(result);
        sample.CopyTo(result.AsSpan(configObus.Length));
        return result;
    }

    /// <summary>The item's nclx colour, range included, else the AV1 sequence header's.</summary>
    private static AvifNclx ColorOf(AvifContainer file, HeifItem? item, Av1SequenceHeader seq)
    {
        if (item is not null)
        {
            foreach (var property in item.Properties)
            {
                if (property.Type != AvifContainer.Colr || property.Length < 11) continue;
                var p = file.PropertyData(property);
                if (BinaryPrimitives.ReadUInt32BigEndian(p) != AvifContainer.Nclx) continue;
                var nclx = new AvifNclx(BinaryPrimitives.ReadUInt16BigEndian(p[4..]), BinaryPrimitives.ReadUInt16BigEndian(p[6..]),
                    BinaryPrimitives.ReadUInt16BigEndian(p[8..]), (p[10] & 0x80) != 0);
                // An nclx that leaves primaries, transfer and matrix unspecified says nothing reliable, its range flag
                // included (early writers set it regardless), so the bitstream's description is used, as libheif does.
                if (nclx is { ColorPrimaries: 2, TransferCharacteristics: 2, MatrixCoefficients: 2 }) break;
                return nclx;
            }
        }
        return new AvifNclx(seq.ColorPrimaries, seq.TransferCharacteristics, seq.MatrixCoefficients, seq.FullRange);
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
        var cropW = widthN / widthD;
        var cropH = heightN / heightD;
        // The aperture is centred on the image centre plus the offset.
        var left = horizN / horizD + (width - cropW) / 2;
        var top = vertN / vertD + (height - cropH) / 2;
        var rect = new Rectangle((int)Math.Round(left), (int)Math.Round(top), (int)Math.Round(cropW), (int)Math.Round(cropH));
        if (rect.Width <= 0 || rect.Height <= 0 || rect.X < 0 || rect.Y < 0 || rect.Right > width || rect.Bottom > height) return null;
        return rect;
    }

    private static string FourCcName(uint type) => new([(char)(type >> 24), (char)((type >> 16) & 0xFF), (char)((type >> 8) & 0xFF), (char)(type & 0xFF)]);
}
