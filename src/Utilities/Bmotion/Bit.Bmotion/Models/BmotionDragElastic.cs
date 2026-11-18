namespace Bit.Bmotion;

/// <summary>
/// Elasticity when a drag exceeds its constraints: 0 = rigid, 1 = fully elastic.
/// Implicitly converts from a uniform <c>double</c> (<c>DragElastic="0.5"</c>), or use
/// <see cref="Edges"/> for per-edge values (motion.dev's object form):
/// <code>
/// DragElastic="BmotionDragElastic.Edges(right: 0.8, bottom: 0.8)"   // other edges rigid
/// </code>
/// </summary>
public readonly struct BmotionDragElastic : IEquatable<BmotionDragElastic>
{
    private BmotionDragElastic(double left, double right, double top, double bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    /// <summary>Elasticity past the left constraint edge.</summary>
    public double Left { get; }

    /// <summary>Elasticity past the right constraint edge.</summary>
    public double Right { get; }

    /// <summary>Elasticity past the top constraint edge.</summary>
    public double Top { get; }

    /// <summary>Elasticity past the bottom constraint edge.</summary>
    public double Bottom { get; }

    /// <summary>Rigid on every edge (no overflow past constraints).</summary>
    public static BmotionDragElastic None => default;

    /// <summary>The same elasticity on every edge.</summary>
    public static BmotionDragElastic Uniform(double value) => new(value, value, value, value);

    /// <summary>Per-edge elasticity; unspecified edges are rigid (0).</summary>
    public static BmotionDragElastic Edges(double left = 0, double right = 0, double top = 0, double bottom = 0)
        => new(left, right, top, bottom);

    public static implicit operator BmotionDragElastic(double value) => Uniform(value);

    public bool Equals(BmotionDragElastic other)
        => Left.Equals(other.Left) && Right.Equals(other.Right)
        && Top.Equals(other.Top) && Bottom.Equals(other.Bottom);

    public override bool Equals(object? obj) => obj is BmotionDragElastic e && Equals(e);
    public override int GetHashCode() => HashCode.Combine(Left, Right, Top, Bottom);
    public static bool operator ==(BmotionDragElastic left, BmotionDragElastic right) => left.Equals(right);
    public static bool operator !=(BmotionDragElastic left, BmotionDragElastic right) => !left.Equals(right);

    /// <summary>
    /// The JS-bridge shape: each edge sanitised to a finite value in [0, 1] (non-finite input
    /// falls back to the 0.35 default, matching the historical uniform behaviour).
    /// </summary>
    internal Dictionary<string, object?> ToJsObject() => new()
    {
        ["left"] = Sanitise(Left),
        ["right"] = Sanitise(Right),
        ["top"] = Sanitise(Top),
        ["bottom"] = Sanitise(Bottom),
    };

    private static double Sanitise(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0.35;
}
