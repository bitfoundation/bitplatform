namespace Bit.Bmage;

/// <summary>The result of encoding an image.</summary>
public sealed class BmageEncodedImage
{
    public BmageEncodedImage(byte[] data, BmageImageFormat format, int width, int height, double? quality = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Format = format;
        Width = width;
        Height = height;
        Quality = quality;
    }

    /// <summary>The encoded bytes.</summary>
    public byte[] Data { get; }
    public BmageImageFormat Format { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>The quality actually used (may differ from the requested one when a size target was in effect).</summary>
    public double? Quality { get; }
    public string MimeType => Format.GetMimeType();
    public string Extension => Format.GetExtension();
    public long Length => Data.LongLength;

    /// <summary>Returns a data URL (base64). Use sparingly: this is expensive for large images.</summary>
    public string ToDataUrl() => $"data:{MimeType};base64,{Convert.ToBase64String(Data)}";

    public Stream AsStream() => new MemoryStream(Data, writable: false);
}
