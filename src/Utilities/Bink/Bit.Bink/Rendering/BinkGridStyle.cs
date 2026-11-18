namespace Bit.Bink;

/// <summary>How <see cref="BinkGrid"/> is drawn.</summary>
public enum BinkGridStyle : byte
{
    /// <summary>Lines, with every few drawn stronger: graph paper.</summary>
    Lines = 0,
    /// <summary>A dot at every intersection: a whiteboard's dotted grid.</summary>
    Dots = 1,
}
