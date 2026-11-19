namespace Bit.BbScanner;

/// <summary>What <see cref="BbScannerStructuredAppendAssembler.Add"/> did with a symbol.</summary>
public enum BbScannerStructuredAppendStatus
{
    /// <summary>The symbol is not part of a structured append sequence; handle it as it is.</summary>
    NotStructuredAppend,

    /// <summary>The symbol was added to its sequence, which still waits for others.</summary>
    Added,

    /// <summary>
    /// The symbol had been added already, to a sequence still waiting or to one completed within the last
    /// <see cref="BbScannerStructuredAppendAssembler.Timeout"/>; nothing changed.
    /// </summary>
    Duplicate,

    /// <summary>The symbol was the last its sequence needed; the message is returned.</summary>
    Completed,

    /// <summary>
    /// The symbol contradicted its sequence - a different symbol in the same place, or a different count - so the
    /// sequence was dropped and a new one started with this symbol.
    /// </summary>
    Conflict,

    /// <summary>
    /// The symbol cannot be used: its structured append header is impossible - a place beyond its count or beyond
    /// <see cref="BbScannerStructuredAppendAssembler.MaxSegments"/>, a Macro PDF417 segment without a file ID - it contradicts
    /// itself, or it completed a QR Code sequence whose parity does not match, which was then dropped. The symbol holds
    /// part of a message, so it is not to be used on its own either.
    /// </summary>
    Rejected,
}
