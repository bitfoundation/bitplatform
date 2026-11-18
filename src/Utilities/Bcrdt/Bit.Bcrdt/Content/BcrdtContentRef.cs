namespace Bit.Bcrdt;

/// <summary>Wire discriminators for block payloads.</summary>
/// <remarks>
/// These values are persisted. Never renumber an existing entry; new payload kinds take the next
/// free number, and readers reject numbers they do not recognise rather than guessing.
/// </remarks>
public enum BcrdtContentRef : byte
{
    /// <summary>A garbage-collected clock range. Not a content kind - a struct kind.</summary>
    GarbageCollected = 0,
    /// <summary>A deleted range whose payload has been dropped.</summary>
    Deleted = 1,
    /// <summary>An opaque byte array.</summary>
    Binary = 2,
    /// <summary>A run of text.</summary>
    String = 3,
    /// <summary>A non-text object embedded inside text.</summary>
    Embed = 4,
    /// <summary>A rich-text mark boundary. Occupies clock space but no index space.</summary>
    Format = 5,
    /// <summary>A nested collaborative type.</summary>
    Type = 6,
    /// <summary>One or more values from the collaborative value model.</summary>
    Any = 7,
    /// <summary>Reserved for nested sub-documents.</summary>
    Doc = 8,
    /// <summary>A deliberately omitted range. Not a content kind - a struct kind.</summary>
    Skip = 9,
}
