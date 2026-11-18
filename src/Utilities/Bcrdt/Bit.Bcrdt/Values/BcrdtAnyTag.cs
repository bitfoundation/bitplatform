namespace Bit.Bcrdt;

/// <summary>Wire tags for the collaborative value model.</summary>
/// <remarks>
/// These numbers are part of the persisted format. Existing tags must never be renumbered or
/// reused; new value kinds take the next free number so that an old reader fails cleanly on a new
/// document rather than misinterpreting it.
/// </remarks>
public enum BcrdtAnyTag : byte
{
    /// <summary>No value at all - distinct from <see cref="Null"/>, used for "key absent".</summary>
    Undefined = 0,
    /// <summary>An explicit null.</summary>
    Null = 1,
    /// <summary>A 64-bit signed integer, zigzag varint encoded.</summary>
    Int = 2,
    /// <summary>An IEEE-754 double.</summary>
    Float64 = 3,
    /// <summary>An IEEE-754 single.</summary>
    Float32 = 4,
    /// <summary>Boolean true. The value is carried by the tag itself.</summary>
    True = 5,
    /// <summary>Boolean false.</summary>
    False = 6,
    /// <summary>A UTF-8 string.</summary>
    String = 7,
    /// <summary>An opaque byte array.</summary>
    Binary = 8,
    /// <summary>An ordered list of values.</summary>
    Array = 9,
    /// <summary>A string-keyed map of values, always written with sorted keys.</summary>
    Object = 10,
    /// <summary>A <see cref="DateTimeOffset"/> as UTC ticks plus an offset in minutes.</summary>
    Timestamp = 11,
    /// <summary>A <see cref="Guid"/> as sixteen raw bytes.</summary>
    Guid = 12,
    /// <summary>A <see cref="decimal"/> as its four constituent 32-bit words.</summary>
    Decimal = 13,
}
