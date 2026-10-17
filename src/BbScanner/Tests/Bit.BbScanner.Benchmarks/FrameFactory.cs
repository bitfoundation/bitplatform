using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Benchmarks;

/// <summary>
/// Builds the frames the benchmarks decode.
/// </summary>
/// <remarks>
/// Benchmarks run against realistic camera frames rather than tightly cropped symbols: a symbol
/// centred in a larger, slightly noisy frame is what the detector actually has to cope with, and
/// it is what determines whether the pipeline fits inside a frame budget.
/// </remarks>
public static class FrameFactory
{
    /// <summary>A rendered frame plus what it should decode to.</summary>
    /// <param name="Name">Human readable case name.</param>
    /// <param name="Format">The symbology in the frame.</param>
    /// <param name="Pixels">Grayscale pixels.</param>
    /// <param name="Width">Frame width.</param>
    /// <param name="Height">Frame height.</param>
    /// <param name="Expected">The value the frame encodes, or null for an empty frame.</param>
    public sealed record Frame(string Name, BbBarcodeFormat Format, byte[] Pixels, int Width, int Height, string? Expected);

    /// <summary>Renders a QR code centred in a camera sized frame.</summary>
    /// <param name="content">Value to encode.</param>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    /// <param name="scale">Pixels per module.</param>
    /// <param name="noise">Peak noise amplitude, 0 for a clean render.</param>
    public static Frame QrCode(string content, int width = 640, int height = 480, int scale = 4, int noise = 0)
    {
        using var matrix = QrEncoder.Encode(content, BbQrErrorCorrectionLevel.M, maskPattern: 2);
        using var symbol = SyntheticImage.FromMatrix(matrix, scale);
        return Compose($"QR {matrix.Width}x{matrix.Height} @{scale}px", BbBarcodeFormat.QrCode, symbol, width, height, content, noise);
    }

    /// <summary>Renders a Data Matrix centred in a camera sized frame.</summary>
    /// <param name="content">Value to encode.</param>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    /// <param name="scale">Pixels per module.</param>
    public static Frame DataMatrix(string content, int width = 640, int height = 480, int scale = 5)
    {
        using var matrix = DataMatrixEncoder.Encode(content);
        using var symbol = SyntheticImage.FromMatrix(matrix, scale);
        return Compose($"Data Matrix {matrix.Width}x{matrix.Height}", BbBarcodeFormat.DataMatrix, symbol, width, height, content, 0);
    }

    /// <summary>Renders a linear symbol centred in a camera sized frame.</summary>
    /// <param name="name">Case name.</param>
    /// <param name="format">The symbology.</param>
    /// <param name="modules">Module array.</param>
    /// <param name="expected">The value the modules encode.</param>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    public static Frame Linear(
        string name, BbBarcodeFormat format, bool[] modules, string expected, int width = 640, int height = 480)
    {
        using var symbol = SyntheticImage.FromLinear(modules, scale: 2, height: 120, quietZoneModules: 12);
        return Compose(name, format, symbol, width, height, expected, 0);
    }

    /// <summary>A frame of plain background, the common case while a user is still aiming.</summary>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    public static Frame Empty(int width = 640, int height = 480)
    {
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)232);
        return new Frame("Empty frame", BbBarcodeFormat.None, pixels, width, height, null);
    }

    private static Frame Compose(
        string name,
        BbBarcodeFormat format,
        BbLuminanceBuffer symbol,
        int width,
        int height,
        string expected,
        int noise)
    {
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)232);

        var offsetX = Math.Max(0, (width - symbol.Width) / 2);
        var offsetY = Math.Max(0, (height - symbol.Height) / 2);
        var copyWidth = Math.Min(symbol.Width, width);
        var copyHeight = Math.Min(symbol.Height, height);

        for (var y = 0; y < copyHeight; y++)
        {
            symbol.Pixels.Slice(y * symbol.Width, copyWidth)
                .CopyTo(pixels.AsSpan(((offsetY + y) * width) + offsetX, copyWidth));
        }

        if (noise > 0)
        {
            var random = new Random(7);
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (byte)Math.Clamp(pixels[i] + random.Next(-noise, noise + 1), 0, 255);
            }
        }

        return new Frame(name, format, pixels, width, height, expected);
    }

    /// <summary>Converts a grayscale frame into the RGBA layout a canvas produces.</summary>
    /// <param name="gray">Grayscale pixels.</param>
    public static byte[] ToRgba(byte[] gray)
    {
        var rgba = new byte[gray.Length * 4];
        for (var i = 0; i < gray.Length; i++)
        {
            var o = i * 4;
            rgba[o] = gray[i];
            rgba[o + 1] = gray[i];
            rgba[o + 2] = gray[i];
            rgba[o + 3] = 255;
        }

        return rgba;
    }
}
