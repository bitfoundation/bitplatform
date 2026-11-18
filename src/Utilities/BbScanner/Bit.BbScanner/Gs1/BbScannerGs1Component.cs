namespace Bit.BbScanner;

// Checks holds the names of the checks the value has to pass, split once here rather than on every validation.
internal sealed record BbScannerGs1Component(BbScannerGs1Charset Charset, int MinLength, int MaxLength, bool Optional, string[] Checks)
{
    public bool IsFixed => MinLength == MaxLength;
}
