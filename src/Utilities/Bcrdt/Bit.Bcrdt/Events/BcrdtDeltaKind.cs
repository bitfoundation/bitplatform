namespace Bit.Bcrdt;

/// <summary>The three things a delta operation can do to a sequence.</summary>
public enum BcrdtDeltaKind
{
    /// <summary>Advance the cursor, leaving content untouched.</summary>
    Retain,
    /// <summary>Insert new content at the cursor.</summary>
    Insert,
    /// <summary>Remove content at the cursor.</summary>
    Delete,
}
