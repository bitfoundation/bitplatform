namespace Bit.BlazorUI;

/// <summary>How pages are paired into spreads, the way a printed book falls open.</summary>
public enum BitPdfSpreadMode
{
    /// <summary>No pairing: one page per row (the default).</summary>
    None,

    /// <summary>Odd-numbered pages start a spread, pairing 1-2, 3-4 and so on.</summary>
    Odd,

    /// <summary>Even-numbered pages start a spread, so page 1 stands alone and 2-3, 4-5 and so on are paired.</summary>
    Even,
}
