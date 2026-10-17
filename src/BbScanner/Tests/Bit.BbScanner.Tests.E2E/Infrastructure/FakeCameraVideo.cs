using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Tests.E2E.Infrastructure;

/// <summary>
/// Writes the Y4M video files that stand in for a camera in the browser.
/// </summary>
/// <remarks>
/// Chromium's <c>--use-file-for-fake-video-capture</c> replaces the camera with a file and loops
/// it forever, which is what makes a real, end to end test of continuous scanning possible: the
/// page opens a real <c>MediaStream</c>, the real frame loop runs, and the real decoder sees the
/// real pixels the browser produced. Only the sensor is synthetic.
/// </remarks>
public static class FakeCameraVideo
{
    /// <summary>Frames per second declared in the file header.</summary>
    public const int FrameRate = 10;

    /// <summary>Payload of the QR code in the standard clip.</summary>
    public const string QrPayload = "E2E-QR-PAYLOAD";

    /// <summary>Payload of the Code 128 symbol in the standard clip.</summary>
    public const string Code128Payload = "E2E-128";

    /// <summary>Payload of the Data Matrix symbol in the standard clip.</summary>
    public const string DataMatrixPayload = "E2E-DM";

    /// <summary>Payload of the EAN-13 symbol in the standard clip.</summary>
    public const string Ean13Payload = "4006381333931";

    /// <summary>
    /// Writes a looping clip that shows, in turn, a QR code, a Data Matrix, a Code 128 and an
    /// EAN-13, with blank frames between them so that duplicate suppression is exercised too.
    /// </summary>
    /// <param name="path">File to write.</param>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    /// <param name="framesPerSymbol">How many frames each symbol is held for.</param>
    public static void WriteRotatingSymbols(string path, int width = 640, int height = 480, int framesPerSymbol = 12)
    {
        using var qr = SyntheticImage.FromMatrix(QrEncoder.Encode(QrPayload, BbQrErrorCorrectionLevel.M, 2), scale: 6);
        using var dm = SyntheticImage.FromMatrix(DataMatrixEncoder.Encode(DataMatrixPayload), scale: 8);
        using var c128 = SyntheticImage.FromLinear(LinearEncoders.Code128(Code128Payload), scale: 3, height: 120);
        using var ean = SyntheticImage.FromLinear(LinearEncoders.Ean13(Ean13Payload), scale: 3, height: 120);

        using var stream = File.Create(path);
        WriteHeader(stream, width, height);

        var frame = BbLuminanceBuffer.Rent(width, height);
        try
        {
            foreach (var symbol in new[] { qr, dm, c128, ean })
            {
                for (var i = 0; i < framesPerSymbol; i++)
                {
                    Compose(frame, symbol, width, height);
                    WriteFrame(stream, frame, width, height);
                }

                // A few blank frames, so a scanner that reports on every frame is distinguishable
                // from one that reports on every symbol.
                for (var i = 0; i < 3; i++)
                {
                    frame.Pixels.Fill(235);
                    WriteFrame(stream, frame, width, height);
                }
            }
        }
        finally
        {
            frame.Dispose();
        }
    }

    /// <summary>Writes a clip that contains no barcode at all.</summary>
    /// <param name="path">File to write.</param>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    public static void WriteBlank(string path, int width = 640, int height = 480)
    {
        using var stream = File.Create(path);
        WriteHeader(stream, width, height);

        var frame = BbLuminanceBuffer.Rent(width, height);
        try
        {
            for (var i = 0; i < 8; i++)
            {
                frame.Pixels.Fill((byte)(200 + (i * 4)));
                WriteFrame(stream, frame, width, height);
            }
        }
        finally
        {
            frame.Dispose();
        }
    }

    /// <summary>Writes a clip showing one symbol only, held for the whole clip.</summary>
    /// <param name="path">File to write.</param>
    /// <param name="payload">QR payload to show.</param>
    /// <param name="width">Frame width.</param>
    /// <param name="height">Frame height.</param>
    public static void WriteSingleQr(string path, string payload, int width = 640, int height = 480)
    {
        using var qr = SyntheticImage.FromMatrix(QrEncoder.Encode(payload, BbQrErrorCorrectionLevel.M, 2), scale: 6);
        using var stream = File.Create(path);
        WriteHeader(stream, width, height);

        var frame = BbLuminanceBuffer.Rent(width, height);
        try
        {
            for (var i = 0; i < 8; i++)
            {
                Compose(frame, qr, width, height);
                WriteFrame(stream, frame, width, height);
            }
        }
        finally
        {
            frame.Dispose();
        }
    }

    private static void Compose(BbLuminanceBuffer frame, BbLuminanceBuffer symbol, int width, int height)
    {
        frame.Pixels.Fill(235);
        ImageTransforms.Paste(symbol, frame, (width - symbol.Width) / 2, (height - symbol.Height) / 2);
    }

    private static void WriteHeader(Stream stream, int width, int height)
    {
        var header = $"YUV4MPEG2 W{width} H{height} F{FrameRate}:1 Ip A1:1 C420jpeg\n";
        stream.Write(System.Text.Encoding.ASCII.GetBytes(header));
    }

    private static void WriteFrame(Stream stream, BbLuminanceBuffer frame, int width, int height)
    {
        stream.Write("FRAME\n"u8);

        // Y plane straight from the luminance buffer.
        for (var y = 0; y < height; y++)
        {
            stream.Write(frame.Array.AsSpan(y * width, width));
        }

        // Neutral chroma: the clip is greyscale.
        var chroma = new byte[(width / 2) * (height / 2)];
        Array.Fill(chroma, (byte)128);
        stream.Write(chroma);
        stream.Write(chroma);
    }
}
