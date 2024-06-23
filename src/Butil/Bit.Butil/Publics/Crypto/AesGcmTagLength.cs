namespace Bit.Butil;

/// <summary>
/// The recommended tagLength number values in Bytes with the corresponding bits value.
/// </summary>
public enum AesGcmTagLength
{
    Four = 32,
    Eight = 64,
    Twelve = 96,
    Thirteen = 104,
    Fourteen = 112,
    Fifteen = 120,
    Sixteen = 128
}
