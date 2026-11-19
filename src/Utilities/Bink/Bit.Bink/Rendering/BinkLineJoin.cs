
namespace Bit.Bink;

/// <summary>Shape of the corners of a stroked path.</summary>
public enum BinkLineJoin : byte
{
    /// <summary>Sharp corner.</summary>
    Miter = 0,
    /// <summary>Rounded corner.</summary>
    Round = 1,
    /// <summary>Cut-off corner.</summary>
    Bevel = 2,
}
