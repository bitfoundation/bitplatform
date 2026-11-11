using System.Globalization;
using System.Text;

namespace Bit.Bink;

/// <summary>The standard PDF faces text is set in. Every viewer has them, so nothing is embedded.</summary>
internal enum BinkPdfFace : byte
{
    Helvetica,
    Times,
    Courier,
}

/// <summary>
/// The resources every page and form of a PDF export shares: graphics states, fonts, images and forms, written once
/// as one dictionary that every content stream refers to.
/// </summary>
internal sealed class BinkPdfResources
{
    private readonly BinkPdfFile _file;
    private readonly long _maxImagePixels;
    private readonly Dictionary<(int Fill, int Stroke, BinkBlendMode Blend), string> _states = new();
    private readonly Dictionary<int, string> _maskStates = new();
    private readonly List<(string Name, string Body)> _stateBodies = new();
    private readonly Dictionary<string, int> _fonts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string? Name, int Width, int Height, int Orientation)> _images = new(StringComparer.Ordinal);
    private readonly List<(string Name, int Id)> _xObjects = new();

    public BinkPdfResources(BinkPdfFile file, long maxImagePixels)
    {
        _file = file;
        _maxImagePixels = maxImagePixels;
        Id = file.Reserve();
    }

    /// <summary>The object number of the shared resource dictionary.</summary>
    public int Id { get; }

    /// <summary>Images that could not be drawn, counted each time one is drawn.</summary>
    public int SkippedImages { get; set; }

    /// <summary>A graphics state with the given fill and stroke alpha and blend mode, or null when it is the default.</summary>
    public string? State(float fillAlpha, float strokeAlpha, BinkBlendMode blend)
    {
        int fill = (int)MathF.Round(Math.Clamp(fillAlpha, 0f, 1f) * 1000f);
        int stroke = (int)MathF.Round(Math.Clamp(strokeAlpha, 0f, 1f) * 1000f);
        if (blend is not (BinkBlendMode.Multiply or BinkBlendMode.Screen)) blend = BinkBlendMode.Normal;
        if (fill == 1000 && stroke == 1000 && blend == BinkBlendMode.Normal) return null;
        if (_states.TryGetValue((fill, stroke, blend), out string? name)) return name;
        name = "G" + _stateBodies.Count.ToString(CultureInfo.InvariantCulture);
        string mode = blend == BinkBlendMode.Normal ? "" : " /BM /" + blend;
        _stateBodies.Add((name, $"<< /Type /ExtGState /ca {BinkPdfFile.Number(fill / 1000f)} /CA {BinkPdfFile.Number(stroke / 1000f)}{mode} >>"));
        _states[(fill, stroke, blend)] = name;
        return name;
    }

    /// <summary>A graphics state whose soft mask is the luminosity of the form <paramref name="maskForm"/>.</summary>
    public string MaskState(int maskForm)
    {
        if (_maskStates.TryGetValue(maskForm, out string? name)) return name;
        name = "G" + _stateBodies.Count.ToString(CultureInfo.InvariantCulture);
        _stateBodies.Add((name, $"<< /Type /ExtGState /SMask << /Type /Mask /S /Luminosity /G {maskForm} 0 R >> >>"));
        _maskStates[maskForm] = name;
        return name;
    }

    /// <summary>Adds a form: content drawn in document coordinates, as a transparency group when <paramref name="group"/> is set.</summary>
    /// <returns>The form's resource name and object number.</returns>
    public (string Name, int Id) Form(ReadOnlySpan<byte> content, BinkRect bbox, string? group)
    {
        string dictionary = $"/Type /XObject /Subtype /Form /BBox [{BinkPdfFile.Number(bbox.Left)} {BinkPdfFile.Number(bbox.Top)} {BinkPdfFile.Number(bbox.Right)} {BinkPdfFile.Number(bbox.Bottom)}] /Resources {Id} 0 R";
        if (group is not null) dictionary += " /Group " + group;
        int id = _file.AddStream(dictionary, content);
        string name = "X" + _xObjects.Count.ToString(CultureInfo.InvariantCulture);
        _xObjects.Add((name, id));
        return (name, id);
    }

    /// <summary>The resource name of a standard font.</summary>
    public string Font(BinkPdfFace face, bool bold, bool italic)
    {
        string baseFont = face switch
        {
            BinkPdfFace.Times => (bold, italic) switch { (true, true) => "Times-BoldItalic", (true, false) => "Times-Bold", (false, true) => "Times-Italic", _ => "Times-Roman" },
            BinkPdfFace.Courier => (bold, italic) switch { (true, true) => "Courier-BoldOblique", (true, false) => "Courier-Bold", (false, true) => "Courier-Oblique", _ => "Courier" },
            _ => (bold, italic) switch { (true, true) => "Helvetica-BoldOblique", (true, false) => "Helvetica-Bold", (false, true) => "Helvetica-Oblique", _ => "Helvetica" },
        };
        if (!_fonts.ContainsKey(baseFont)) _fonts[baseFont] = _file.Add($"<< /Type /Font /Subtype /Type1 /BaseFont /{baseFont} /Encoding /WinAnsiEncoding >>");
        return "F-" + baseFont;
    }

    /// <summary>
    /// The resource name of an embedded image and its size in pixels, or null when it cannot be put in the file. Each
    /// picture is written once however often it is drawn.
    /// </summary>
    /// <param name="source">The picture.</param>
    /// <param name="width">Its width as displayed, after its EXIF orientation, in pixels.</param>
    /// <param name="height">Its height as displayed, in pixels.</param>
    /// <param name="orientation">Its EXIF orientation, 1 to 8: how the stored pixels turn to be displayed.</param>
    public string? Image(BinkImageSource source, out int width, out int height, out int orientation)
    {
        if (!_images.TryGetValue(source.Uri, out var image))
        {
            image = CreateImage(source);
            _images[source.Uri] = image;
        }
        width = image.Width;
        height = image.Height;
        orientation = image.Orientation;
        return image.Name;
    }

    private (string? Name, int Width, int Height, int Orientation) CreateImage(BinkImageSource source)
    {
        if (!source.TryGetBytes(out byte[] bytes)) return (null, 0, 0, 1);
        int id;
        int width, height;
        int orientation = 1;
        if (TryReadJpeg(bytes, out width, out height, out int components, out orientation))
        {
            if (components is not (1 or 3) || (long)width * height > _maxImagePixels) return (null, 0, 0, 1);
            string space = components == 1 ? "/DeviceGray" : "/DeviceRGB";
            id = _file.AddRawStream($"/Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace {space} /BitsPerComponent 8 /Filter /DCTDecode", bytes);
            // A browser displays a photo turned as its EXIF orientation says, which PDF's DCT filter ignores; the
            // image is placed turned instead, and its size is the size it is displayed at.
            if (orientation >= 5) (width, height) = (height, width);
        }
        else if (BinkPngCodec.IsPng(bytes))
        {
            if (!BinkPngCodec.TryReadSize(bytes, out width, out height) || (long)width * height > _maxImagePixels) return (null, 0, 0, 1);
            BinkPngCodec.DecodedImage decoded;
            try
            {
                decoded = BinkPngCodec.Decode(bytes, _maxImagePixels);
            }
            catch (Exception ex) when (ex is FormatException or InvalidDataException or InvalidOperationException or ArgumentException or OverflowException or NotSupportedException)
            {
                return (null, 0, 0, 1);
            }
            (width, height) = (decoded.Width, decoded.Height);
            int pixels = width * height;
            byte[] rgba = decoded.Rgba;
            // The decoded pixels are split in place rather than copied: the colour is packed into the front of the
            // same buffer (each pixel is written at or before where it was read), and only the alpha, a quarter of
            // the size, gets a buffer of its own, and only when the picture is translucent. Peak memory is the decoded
            // image plus at most a quarter of it, however well the PNG compressed.
            bool translucent = false;
            for (int i = 3; i < rgba.Length && !translucent; i += 4) translucent = rgba[i] != 255;
            byte[]? alpha = translucent ? new byte[pixels] : null;
            for (int i = 0; i < pixels; i++)
            {
                if (alpha is not null) alpha[i] = rgba[i * 4 + 3];
                rgba[i * 3] = rgba[i * 4];
                rgba[i * 3 + 1] = rgba[i * 4 + 1];
                rgba[i * 3 + 2] = rgba[i * 4 + 2];
            }
            string mask = "";
            if (alpha is not null)
            {
                int maskId = _file.AddStream($"/Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceGray /BitsPerComponent 8", alpha);
                mask = $" /SMask {maskId} 0 R";
            }
            id = _file.AddStream($"/Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8{mask}", rgba.AsSpan(0, pixels * 3));
        }
        else
        {
            return (null, 0, 0, 1);
        }
        string name = "I" + _xObjects.Count.ToString(CultureInfo.InvariantCulture);
        _xObjects.Add((name, id));
        return (name, width, height, orientation);
    }

    /// <summary>
    /// Reads a baseline or progressive JPEG's stored size and colour components from its frame header, and its EXIF
    /// orientation (1 when it has none) from an APP1 segment before it. False unless a scan follows the frame and the
    /// image ends after it, so a header with no picture behind it is not copied into the file.
    /// </summary>
    private static bool TryReadJpeg(ReadOnlySpan<byte> data, out int width, out int height, out int components, out int orientation)
    {
        width = height = components = 0;
        orientation = 1;
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8) return false;
        bool sawExif = false, sawFrame = false;
        int i = 2;
        while (i + 4 <= data.Length)
        {
            if (data[i] != 0xFF) return false;
            byte marker = data[i + 1];
            if (marker == 0xFF) { i++; continue; }
            if (marker is 0x01 or (>= 0xD0 and <= 0xD7)) { i += 2; continue; }
            if (marker == 0xD9) return false;
            int length = (data[i + 2] << 8) | data[i + 3];
            if (length < 2 || i + 2 + length > data.Length) return false;
            // Start of frame: baseline, extended or progressive Huffman coding, which PDF's DCT filter reads.
            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                if (sawFrame || length < 8 || data[i + 4] != 8) return false;
                height = (data[i + 5] << 8) | data[i + 6];
                width = (data[i + 7] << 8) | data[i + 8];
                components = data[i + 9];
                if (width <= 0 || height <= 0) return false;
                sawFrame = true;
            }
            if (marker == 0xDA)
            {
                // A frame is only a picture once a scan follows it: a scan header, coded data, and the end of the
                // image after it. Coded data never holds 0xFF 0xD9, a 0xFF in it being followed by 0x00 or a restart.
                if (!sawFrame || length < 6) return false;
                int scan = i + 2 + length;
                return scan < data.Length && data[scan..].IndexOf((ReadOnlySpan<byte>)[0xFF, 0xD9]) > 0;
            }
            // The first Exif segment decides, as browsers read it; a later one (an XMP packet shares the marker) does not.
            if (marker == 0xE1 && !sawExif && data.Slice(i + 4, length - 2).StartsWith("Exif\0\0"u8))
            {
                sawExif = true;
                orientation = ReadExifOrientation(data.Slice(i + 4, length - 2));
            }
            i += 2 + length;
        }
        return false;
    }

    /// <summary>The orientation tag of an APP1 Exif segment's first image directory, or 1 when it has none or is malformed.</summary>
    private static int ReadExifOrientation(ReadOnlySpan<byte> segment)
    {
        // "Exif\0\0", then a TIFF header: byte order, 42, and the offset of the first directory.
        if (segment.Length < 14 || !segment[..6].SequenceEqual("Exif\0\0"u8)) return 1;
        ReadOnlySpan<byte> tiff = segment[6..];
        bool little = tiff[0] == 'I' && tiff[1] == 'I';
        if (!little && !(tiff[0] == 'M' && tiff[1] == 'M')) return 1;
        static int Read16(ReadOnlySpan<byte> tiff, int at, bool little) => little ? tiff[at] | (tiff[at + 1] << 8) : (tiff[at] << 8) | tiff[at + 1];
        if (Read16(tiff, 2, little) != 42) return 1;
        long directory = little
            ? (uint)(tiff[4] | (tiff[5] << 8) | (tiff[6] << 16) | (tiff[7] << 24))
            : (uint)((tiff[4] << 24) | (tiff[5] << 16) | (tiff[6] << 8) | tiff[7]);
        if (directory < 8 || directory + 2 > tiff.Length) return 1;
        int count = Read16(tiff, (int)directory, little);
        for (int e = 0; e < count; e++)
        {
            int entry = (int)directory + 2 + e * 12;
            if (entry + 12 > tiff.Length) return 1;
            // Tag 0x0112, type SHORT, one value, held in the entry itself.
            if (Read16(tiff, entry, little) != 0x0112) continue;
            int value = Read16(tiff, entry + 8, little);
            return Read16(tiff, entry + 2, little) == 3 && value is >= 1 and <= 8 ? value : 1;
        }
        return 1;
    }

    /// <summary>Writes the shared resource dictionary and the graphics states it names.</summary>
    public void Write()
    {
        var builder = new StringBuilder("<<");
        if (_stateBodies.Count > 0)
        {
            builder.Append(" /ExtGState <<");
            foreach ((string name, string body) in _stateBodies) builder.Append(CultureInfo.InvariantCulture, $" /{name} {_file.Add(body)} 0 R");
            builder.Append(" >>");
        }
        if (_xObjects.Count > 0)
        {
            builder.Append(" /XObject <<");
            foreach ((string name, int id) in _xObjects) builder.Append(CultureInfo.InvariantCulture, $" /{name} {id} 0 R");
            builder.Append(" >>");
        }
        if (_fonts.Count > 0)
        {
            builder.Append(" /Font <<");
            foreach ((string baseFont, int id) in _fonts) builder.Append(CultureInfo.InvariantCulture, $" /F-{baseFont} {id} 0 R");
            builder.Append(" >>");
        }
        builder.Append(" >>");
        _file.Set(Id, builder.ToString());
    }
}
