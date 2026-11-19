namespace Bit.BbScanner;

internal sealed record BbScannerGs1AiDefinition(
    string Ai,
    string Title,
    BbScannerGs1Component[] Components,
    BbScannerGs1ValueKind Kind,
    int? DecimalPlaces)
{
    /// <summary>The length of the data field when every component, optional ones included, is fixed and present.</summary>
    public int? FixedLength
    {
        get
        {
            var length = 0;
            foreach (var component in Components)
            {
                if (!component.IsFixed || component.Optional)
                {
                    return null;
                }

                length += component.MaxLength;
            }

            return length;
        }
    }
}
