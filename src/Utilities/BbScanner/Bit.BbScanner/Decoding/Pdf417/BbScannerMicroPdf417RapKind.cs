namespace Bit.BbScanner;

/// <summary>Where in a row a row address pattern sits.</summary>
public enum BbScannerMicroPdf417RapKind
{
    /// <summary>At the start of the row.</summary>
    Left,

    /// <summary>Between the columns of a three or four column symbol.</summary>
    Centre,

    /// <summary>At the end of the row, before its closing bar.</summary>
    Right,
}
