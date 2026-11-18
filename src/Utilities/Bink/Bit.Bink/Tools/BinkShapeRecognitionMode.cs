
namespace Bit.Bink;

/// <summary>When <see cref="BinkDrawTool"/> turns a stroke into a clean shape.</summary>
public enum BinkShapeRecognitionMode : byte
{
    /// <summary>Strokes stay ink. The default.</summary>
    Off = 0,

    /// <summary>
    /// A stroke becomes a shape when the pen rests at its end for <see cref="BinkDrawTool.ShapeHoldDuration"/> before
    /// lifting, so ink and shapes can be drawn with the same tool.
    /// </summary>
    OnHold = 1,

    /// <summary>Every stroke that is close enough to a shape becomes one.</summary>
    Always = 2,
}
