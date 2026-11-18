
namespace Bit.Bink;

/// <summary>How a drawing operation is composited with what is already on the surface.</summary>
public enum BinkBlendMode : byte
{
    /// <summary>Standard source-over alpha compositing.</summary>
    Normal = 0,
    /// <summary>Multiplies colors: what highlighters do on paper. Overlapping highlights get darker, black stays black.</summary>
    Multiply = 1,
    /// <summary>Screen (inverse multiply); lightens.</summary>
    Screen = 2,
    /// <summary>Erases: removes existing content where the shape is drawn (within its layer). Used by the pixel eraser.</summary>
    DestinationOut = 3,
}
