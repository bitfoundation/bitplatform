namespace Bit.BbScanner;

/// <summary>A structured append sequence put back together.</summary>
public sealed class BbScannerStructuredAppendMessage
{
    internal BbScannerStructuredAppendMessage(BbScannerBarcodeFormat format, string text, byte[] rawBytes, BbScannerBarcodeResult[] segments)
    {
        Format = format;
        Text = text;
        RawBytes = rawBytes;
        Segments = segments;
    }

    /// <summary>The symbology of the sequence.</summary>
    public BbScannerBarcodeFormat Format { get; }

    /// <summary>The message: the symbols' texts in sequence order.</summary>
    public string Text { get; }

    /// <summary>The symbols' raw bytes in sequence order.</summary>
    public ReadOnlyMemory<byte> RawBytes { get; }

    /// <summary>The symbols, in sequence order.</summary>
    public IReadOnlyList<BbScannerBarcodeResult> Segments { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Format}: {Text}";
}
