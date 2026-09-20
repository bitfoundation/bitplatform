namespace Bit.Butil;

/// <summary>
/// The recommended tagLength number values in Bytes with the corresponding bits value.
/// </summary>
public enum AesGcmTagLength
{
    /// <summary>32 bits. Only for very short messages, and only when the space really matters.</summary>
    Four = 32,

    /// <summary>64 bits.</summary>
    Eight = 64,

    /// <summary>96 bits.</summary>
    Twelve = 96,

    /// <summary>104 bits.</summary>
    Thirteen = 104,

    /// <summary>112 bits.</summary>
    Fourteen = 112,

    /// <summary>120 bits.</summary>
    Fifteen = 120,
    
    /// <summary>128 bits - the full tag, and the default. Use this unless something forces otherwise.</summary>
    Sixteen = 128
}
