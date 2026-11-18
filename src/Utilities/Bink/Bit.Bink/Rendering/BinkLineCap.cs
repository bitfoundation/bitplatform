
namespace Bit.Bink;

/// <summary>Shape of the ends of a stroked path.</summary>
public enum BinkLineCap : byte
{
    /// <summary>Flat end exactly at the endpoint.</summary>
    Butt = 0,
    /// <summary>Semicircular end.</summary>
    Round = 1,
    /// <summary>Flat end extended by half the line width.</summary>
    Square = 2,
}
