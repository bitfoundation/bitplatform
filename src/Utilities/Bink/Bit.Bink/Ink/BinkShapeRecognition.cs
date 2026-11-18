using System.Numerics;

namespace Bit.Bink;

/// <summary>A stroke recognized as a shape by <see cref="BinkShapeRecognizer"/>.</summary>
public sealed record BinkShapeRecognition
{
    internal BinkShapeRecognition(BinkShapeKind kind, IReadOnlyList<Vector2> points, BinkRect frame, float rotation, float error)
    {
        Kind = kind;
        Points = points;
        Frame = frame;
        Rotation = rotation;
        Error = error;
    }

    /// <summary>What the stroke was recognized as.</summary>
    public BinkShapeKind Kind { get; }

    /// <summary>
    /// The fitted points in document units: the start and end of a line, the tail and tip of an arrow, the vertices
    /// of a triangle, diamond or polygon, the four corners of a rectangle, and none for an ellipse.
    /// </summary>
    public IReadOnlyList<Vector2> Points { get; }

    /// <summary>
    /// The upright rectangle a rectangle or ellipse fills before <see cref="Rotation"/> turns it about its centre, and
    /// the bounds of the points of every other shape.
    /// </summary>
    public BinkRect Frame { get; }

    /// <summary>The rotation in radians of a rectangle or ellipse about the centre of <see cref="Frame"/>; zero otherwise.</summary>
    public float Rotation { get; }

    /// <summary>
    /// How far the stroke strays from the shape on average, as a fraction of the diagonal of its bounds: zero for a
    /// perfect match, never above <see cref="BinkShapeRecognizerOptions.Tolerance"/>.
    /// </summary>
    public float Error { get; }

    /// <summary>Creates the shape element, in document coordinates, drawn with <paramref name="style"/>.</summary>
    public BinkShapeElement CreateElement(BinkShapeStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        Matrix3x2 transform = Rotation == 0f ? Matrix3x2.Identity : Matrix3x2.CreateRotation(Rotation, Frame.Center);
        return Kind switch
        {
            BinkShapeKind.Line => new BinkLineShape(Points[0], Points[1]) { Style = style },
            BinkShapeKind.Arrow => new BinkLineShape(Points[0], Points[1]) { EndArrow = true, Style = style },
            BinkShapeKind.Rectangle => new BinkRectangleShape(Frame) { Transform = transform, Style = style },
            BinkShapeKind.Ellipse => new BinkEllipseShape(Frame) { Transform = transform, Style = style },
            _ => new BinkPolygonShape(Points) { Style = style },
        };
    }
}
