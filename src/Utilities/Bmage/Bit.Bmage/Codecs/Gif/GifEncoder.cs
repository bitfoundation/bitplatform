using System.Buffers.Binary;

namespace Bit.Bmage.Codecs.Gif;

/// <summary>
/// Managed GIF encoder: one frame, a palette of up to 256 colours (<see cref="ImageExportOptions.MaxColors"/>, chosen
/// by median cut and dithered unless <see cref="ImageExportOptions.Dither"/> is off), and one transparent colour.
/// </summary>
/// <remarks>
/// GIF stores no metadata Bmage reads and no partial transparency: a pixel under half opaque becomes transparent, and
/// the others opaque. An image with no more colours than the palette allows keeps them exactly; a photo does not, which
/// is why PNG or WebP suit photos better. Only the first frame is written: Bmage does not edit animations.
/// </remarks>
public sealed class GifEncoder : IImageEncoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Gif];

    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
        => new(Encode(image, options, cancellationToken));

    /// <summary>Encodes <paramref name="image"/> as a GIF.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The image is wider or taller than GIF's 65535 pixels.</exception>
    public static EncodedImage Encode(ImageBuffer image, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= new ImageExportOptions { Format = ImageFormat.Gif };
        if (image.Width > ushort.MaxValue || image.Height > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(image), $"GIF holds images up to {ushort.MaxValue} pixels either way, not {image.Width} x {image.Height}.");

        var quantized = ColorQuantizer.Quantize(image, options.MaxColors ?? 256, options.Dither, alphaLevels: 2, cancellationToken);
        var palette = quantized.Palette;
        // The colour table holds a power of two entries, at least two.
        var tableBits = 1;
        while ((1 << tableBits) < palette.Length) tableBits++;
        var transparent = Array.FindIndex(palette, c => c.A == 0);

        var ms = new MemoryStream(Math.Max(1024, image.PixelCount / 3));
        ms.Write("GIF89a"u8);
        Span<byte> screen = stackalloc byte[7];
        BinaryPrimitives.WriteUInt16LittleEndian(screen, (ushort)image.Width);
        BinaryPrimitives.WriteUInt16LittleEndian(screen[2..], (ushort)image.Height);
        screen[4] = (byte)(0x80 | (tableBits - 1) << 4 | (tableBits - 1)); // global table, its size, colour resolution
        screen[5] = 0; // background colour index
        screen[6] = 0; // no aspect ratio
        ms.Write(screen);
        for (var i = 0; i < 1 << tableBits; i++)
        {
            var c = i < palette.Length ? palette[i] : default;
            ms.WriteByte(c.R);
            ms.WriteByte(c.G);
            ms.WriteByte(c.B);
        }

        if (transparent >= 0)
        {
            // Graphic control extension: no disposal, no delay, the transparent index.
            ms.Write([0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, (byte)transparent, 0x00]);
        }

        Span<byte> descriptor = stackalloc byte[10];
        descriptor[0] = 0x2C;
        BinaryPrimitives.WriteUInt16LittleEndian(descriptor[1..], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(descriptor[3..], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(descriptor[5..], (ushort)image.Width);
        BinaryPrimitives.WriteUInt16LittleEndian(descriptor[7..], (ushort)image.Height);
        descriptor[9] = 0; // no local table, not interlaced
        ms.Write(descriptor);

        var minimumCodeSize = Math.Max(2, tableBits);
        ms.WriteByte((byte)minimumCodeSize);
        LzwCompress(quantized.Indices, minimumCodeSize, ms, cancellationToken);
        ms.WriteByte(0x3B);
        return new EncodedImage(ms.ToArray(), ImageFormat.Gif, image.Width, image.Height);
    }

    /// <summary>
    /// GIF's variable-length LZW: codes from <c>minimumCodeSize + 1</c> bits up to 12, a clear code when the table is full,
    /// written least significant bit first in sub-blocks of at most 255 bytes.
    /// </summary>
    private static void LzwCompress(byte[] indices, int minimumCodeSize, Stream output, CancellationToken cancellationToken)
    {
        var clear = 1 << minimumCodeSize;
        var end = clear + 1;
        // The table maps (prefix code, next index) to a code; open addressing over a power-of-two array.
        const int tableSize = 1 << 13;
        var keys = new int[tableSize];
        var values = new short[tableSize];
        var blocks = new SubBlockWriter(output);

        int codeSize = minimumCodeSize + 1, next = end + 1;
        void Reset()
        {
            Array.Fill(keys, -1);
            codeSize = minimumCodeSize + 1;
            next = end + 1;
        }

        Reset();
        blocks.WriteCode(clear, codeSize);
        if (indices.Length == 0)
        {
            blocks.WriteCode(end, codeSize);
            blocks.Finish();
            return;
        }

        int prefix = indices[0];
        for (var i = 1; i < indices.Length; i++)
        {
            if ((i & 0xFFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            int symbol = indices[i];
            var key = prefix << 8 | symbol;
            var slot = (int)(((uint)key * 0x9E3779B1u) >> 19) & (tableSize - 1);
            while (keys[slot] != -1 && keys[slot] != key) slot = (slot + 1) & (tableSize - 1);
            if (keys[slot] == key)
            {
                prefix = values[slot];
                continue;
            }

            blocks.WriteCode(prefix, codeSize);
            if (next < 4096)
            {
                keys[slot] = key;
                values[slot] = (short)next;
                // The decoder widens its codes once the next code no longer fits.
                if (next == 1 << codeSize) codeSize++;
                next++;
            }
            else
            {
                blocks.WriteCode(clear, codeSize);
                Reset();
            }
            prefix = symbol;
        }
        blocks.WriteCode(prefix, codeSize);
        // The code just written may have taken the table to the width's limit, which the decoder widens at too.
        if (next < 4096 && next == 1 << codeSize) codeSize++;
        blocks.WriteCode(end, codeSize);
        blocks.Finish();
    }

    /// <summary>Packs codes into bytes, least significant bit first, and the bytes into sub-blocks of up to 255.</summary>
    private sealed class SubBlockWriter(Stream output)
    {
        private readonly byte[] _block = new byte[255];
        private int _used;
        private int _bits;
        private int _pending;

        public void WriteCode(int code, int size)
        {
            _pending |= code << _bits;
            _bits += size;
            while (_bits >= 8)
            {
                Put((byte)_pending);
                _pending >>= 8;
                _bits -= 8;
            }
        }

        public void Finish()
        {
            if (_bits > 0) Put((byte)_pending);
            Flush();
            output.WriteByte(0);
        }

        private void Put(byte value)
        {
            _block[_used++] = value;
            if (_used == _block.Length) Flush();
        }

        private void Flush()
        {
            if (_used == 0) return;
            output.WriteByte((byte)_used);
            output.Write(_block, 0, _used);
            _used = 0;
        }
    }
}
