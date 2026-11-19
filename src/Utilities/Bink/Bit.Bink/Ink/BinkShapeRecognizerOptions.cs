namespace Bit.Bink;

/// <summary>Settings for <see cref="BinkShapeRecognizer"/>.</summary>
public sealed record BinkShapeRecognizerOptions
{
    /// <summary>The settings <see cref="BinkShapeRecognizer"/> uses when given none.</summary>
    public static readonly BinkShapeRecognizerOptions Default = new();

    /// <summary>
    /// How far, on average, a stroke may stray from the shape it is recognized as, as a fraction of the diagonal of its
    /// bounds. Higher accepts sloppier drawing and risks turning ink that was meant as ink into a shape. Default 0.045.
    /// </summary>
    public float Tolerance { get; init; } = 0.045f;

    /// <summary>The smallest diagonal, in document units, a stroke must span to be recognized. Default 12.</summary>
    public float MinimumSize { get; init; } = 12f;

    /// <summary>
    /// Degrees within which a recognized shape is straightened: a line to the nearest multiple of 45 degrees, a
    /// rectangle or ellipse to upright. Zero keeps the angle as drawn. Default 8.
    /// </summary>
    public float AngleSnap { get; init; } = 8f;

    /// <summary>
    /// How much a recognized ellipse's or rectangle's width and height may differ, as a fraction of the larger, and
    /// still be made a circle or a square. Zero keeps the proportions as drawn. Default 0.1.
    /// </summary>
    public float ProportionSnap { get; init; } = 0.1f;

    /// <summary>
    /// The shapes a stroke may become, or null for every one the recognizer knows: <see cref="BinkShapeKind.Line"/>,
    /// <see cref="BinkShapeKind.Arrow"/>, <see cref="BinkShapeKind.Rectangle"/>, <see cref="BinkShapeKind.Ellipse"/>,
    /// <see cref="BinkShapeKind.Triangle"/>, <see cref="BinkShapeKind.Diamond"/> and <see cref="BinkShapeKind.Polygon"/>.
    /// A triangle, diamond or rectangle that is left out may still be recognized as a polygon when that is allowed.
    /// </summary>
    public IReadOnlySet<BinkShapeKind>? Kinds { get; init; }

    internal bool Allows(BinkShapeKind kind) => Kinds is null ? kind is not (BinkShapeKind.Highlight or BinkShapeKind.Star) : Kinds.Contains(kind);
}
