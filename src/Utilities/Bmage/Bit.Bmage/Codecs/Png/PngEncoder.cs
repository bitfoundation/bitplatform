using System.Buffers.Binary;
using System.IO.Compression;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Codecs.Png;

/// <summary>
/// Managed PNG encoder producing 8-bit RGB or RGBA images with adaptive per-row filtering, or, when
/// <see cref="ImageExportOptions.MaxColors"/> is set, indexed images of 1, 2, 4 or 8 bits a pixel with a palette.
/// </summary>
public sealed class PngEncoder : IImageEncoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Png];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
        => new(Encode(image, options, cancellationToken));

    public static EncodedImage Encode(ImageBuffer image, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= ImageExportOptions.Png;
        if (options.MaxColors is { } maxColors)
            return EncodeIndexed(image, ColorQuantizer.Quantize(image, maxColors, options.Dither, cancellationToken: cancellationToken), options, cancellationToken);
        var hasAlpha = image.HasTransparency();
        var channels = hasAlpha ? 4 : 3;
        var width = image.Width;
        var height = image.Height;
        var stride = width * channels;
        var level = Math.Clamp(options.PngCompressionLevel, 0, 9);

        var ms = new MemoryStream(Math.Max(1024, image.ByteLength / 3));
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..], height);
        ihdr[8] = 8; ihdr[9] = (byte)(hasAlpha ? 6 : 2); ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
        PngContainer.WriteChunk(ms, "IHDR", ihdr);

        WriteMetadata(ms, image, options);

        // Filter and compress straight into IDAT chunks. PNG allows the image data to be split across any number of
        // IDAT chunks, so the compressed bytes never have to be held in one buffer; peak memory stays at the chunk size
        // rather than the size of the whole compressed image.
        var compression = level switch { 0 => CompressionLevel.NoCompression, <= 3 => CompressionLevel.Fastest, <= 7 => CompressionLevel.Optimal, _ => CompressionLevel.SmallestSize };
        using (var idat = new IdatChunkWriter(ms))
        using (var zs = new ZLibStream(idat, compression, leaveOpen: true))
        {
            var prev = new byte[stride];
            var cur = new byte[stride];
            var filtered = new byte[stride + 1];
            var candidate = new byte[stride + 1];
            var adaptive = level >= 2;
            for (var y = 0; y < height; y++)
            {
                if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
                var row = image.GetRow(y);
                if (hasAlpha)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var p = row[x];
                        cur[x * 4] = p.R; cur[x * 4 + 1] = p.G; cur[x * 4 + 2] = p.B; cur[x * 4 + 3] = p.A;
                    }
                }
                else
                {
                    for (var x = 0; x < width; x++)
                    {
                        var p = row[x];
                        cur[x * 3] = p.R; cur[x * 3 + 1] = p.G; cur[x * 3 + 2] = p.B;
                    }
                }
                if (adaptive) ChooseFilter(cur, prev, channels, filtered, candidate);
                else { filtered[0] = 0; cur.CopyTo(filtered, 1); }
                zs.Write(filtered, 0, filtered.Length);
                (prev, cur) = (cur, prev);
            }
        }
        PngContainer.WriteChunk(ms, "IEND", default);
        return new EncodedImage(ms.ToArray(), ImageFormat.Png, width, height);
    }

    private static void WriteMetadata(MemoryStream ms, ImageBuffer image, ImageExportOptions options)
    {
        var width = image.Width;
        var height = image.Height;
        var metadata = image.Metadata?.Apply(options.Metadata, width, height);
        if (metadata is { IsEmpty: false })
        {
            if (!metadata.IccProfile.IsEmpty)
            {
                var body = new MemoryStream();
                body.Write("ICC Profile\0\0"u8);
                using (var zs = new ZLibStream(body, CompressionLevel.Optimal, leaveOpen: true)) zs.Write(metadata.IccProfile.Span);
                PngContainer.WriteChunk(ms, "iCCP", body.ToArray());
            }
            if (metadata.Exif is { IsEmpty: false } exif) PngContainer.WriteChunk(ms, "eXIf", exif.ToTiff());
            if (!metadata.Xmp.IsEmpty)
            {
                var body = new MemoryStream();
                body.Write("XML:com.adobe.xmp\0"u8);
                body.Write([0, 0, 0, 0]);
                body.Write(metadata.Xmp.Span);
                PngContainer.WriteChunk(ms, "iTXt", body.ToArray());
            }
        }
    }

    /// <summary>
    /// Writes an indexed PNG: a PLTE chunk of the palette, a tRNS chunk of the translucent entries' alpha (they come
    /// first), and the indices packed at the fewest bits a pixel the palette allows. Rows are left unfiltered, as the
    /// specification recommends for palette images: neighbouring indices are not neighbouring colours.
    /// </summary>
    internal static EncodedImage EncodeIndexed(ImageBuffer image, QuantizedImage quantized, ImageExportOptions options, CancellationToken cancellationToken)
    {
        int width = image.Width, height = image.Height;
        var palette = quantized.Palette;
        var bits = palette.Length switch { <= 2 => 1, <= 4 => 2, <= 16 => 4, _ => 8 };
        var level = Math.Clamp(options.PngCompressionLevel, 0, 9);

        var ms = new MemoryStream(Math.Max(1024, image.PixelCount / 4));
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..], height);
        ihdr[8] = (byte)bits; ihdr[9] = 3; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
        PngContainer.WriteChunk(ms, "IHDR", ihdr);
        WriteMetadata(ms, image, options);

        var plte = new byte[palette.Length * 3];
        for (var i = 0; i < palette.Length; i++)
        {
            plte[i * 3] = palette[i].R;
            plte[i * 3 + 1] = palette[i].G;
            plte[i * 3 + 2] = palette[i].B;
        }
        PngContainer.WriteChunk(ms, "PLTE", plte);
        var translucent = quantized.TranslucentCount;
        if (translucent > 0)
        {
            var trns = new byte[translucent];
            for (var i = 0; i < translucent; i++) trns[i] = palette[i].A;
            PngContainer.WriteChunk(ms, "tRNS", trns);
        }

        var compression = level switch { 0 => CompressionLevel.NoCompression, <= 3 => CompressionLevel.Fastest, <= 7 => CompressionLevel.Optimal, _ => CompressionLevel.SmallestSize };
        var stride = (width * bits + 7) / 8;
        using (var idat = new IdatChunkWriter(ms))
        using (var zs = new ZLibStream(idat, compression, leaveOpen: true))
        {
            var line = new byte[stride + 1];
            var perByte = 8 / bits;
            for (var y = 0; y < height; y++)
            {
                if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
                var indices = quantized.Indices.AsSpan(y * width, width);
                Array.Clear(line);
                if (bits == 8) indices.CopyTo(line.AsSpan(1));
                else
                    for (var x = 0; x < width; x++)
                        line[1 + x / perByte] |= (byte)(indices[x] << (8 - bits * (x % perByte + 1)));
                zs.Write(line, 0, line.Length);
            }
        }
        PngContainer.WriteChunk(ms, "IEND", default);
        return new EncodedImage(ms.ToArray(), ImageFormat.Png, width, height);
    }

    private static void ChooseFilter(byte[] cur, byte[] prev, int bpp, byte[] best, byte[] candidate)
    {
        var bestSum = long.MaxValue;
        for (byte f = 0; f <= 4; f++)
        {
            candidate[0] = f;
            Filter(f, cur, prev, bpp, candidate.AsSpan(1));
            long sum = 0;
            for (var i = 1; i < candidate.Length; i++)
            {
                var v = (sbyte)candidate[i];
                sum += v < 0 ? -v : v;
                if (sum >= bestSum) break;
            }
            if (sum < bestSum)
            {
                bestSum = sum;
                candidate.CopyTo(best, 0);
            }
        }
    }

    private static void Filter(byte type, ReadOnlySpan<byte> cur, ReadOnlySpan<byte> prev, int bpp, Span<byte> dst)
    {
        switch (type)
        {
            case 0: cur.CopyTo(dst); break;
            case 1:
                for (var i = 0; i < cur.Length; i++) dst[i] = (byte)(cur[i] - (i >= bpp ? cur[i - bpp] : 0));
                break;
            case 2:
                for (var i = 0; i < cur.Length; i++) dst[i] = (byte)(cur[i] - prev[i]);
                break;
            case 3:
                for (var i = 0; i < cur.Length; i++) dst[i] = (byte)(cur[i] - (((i >= bpp ? cur[i - bpp] : 0) + prev[i]) >> 1));
                break;
            case 4:
                for (var i = 0; i < cur.Length; i++)
                {
                    int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                    var p = a + b - c;
                    var pa = Math.Abs(p - a); var pb = Math.Abs(p - b); var pc = Math.Abs(p - c);
                    dst[i] = (byte)(cur[i] - (pa <= pb && pa <= pc ? a : pb <= pc ? b : c));
                }
                break;
        }
    }
}

/// <summary>
/// Buffers compressed bytes and emits them as a sequence of IDAT chunks. Writing many modest chunks instead of one huge
/// one keeps peak memory flat regardless of image size, and every PNG decoder concatenates them transparently.
/// </summary>
internal sealed class IdatChunkWriter : Stream
{
    private const int ChunkSize = 64 * 1024;

    private readonly Stream _output;
    private readonly byte[] _buffer = new byte[ChunkSize];
    private int _used;

    public IdatChunkWriter(Stream output) => _output = output;

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            var room = ChunkSize - _used;
            var take = Math.Min(room, buffer.Length);
            buffer[..take].CopyTo(_buffer.AsSpan(_used));
            _used += take;
            buffer = buffer[take..];
            if (_used == ChunkSize) FlushChunk();
        }
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void WriteByte(byte value)
    {
        _buffer[_used++] = value;
        if (_used == ChunkSize) FlushChunk();
    }

    private void FlushChunk()
    {
        if (_used == 0) return;
        PngContainer.WriteChunk(_output, "IDAT", _buffer.AsSpan(0, _used));
        _used = 0;
    }

    public override void Flush() { }

    protected override void Dispose(bool disposing)
    {
        if (disposing) FlushChunk();
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
