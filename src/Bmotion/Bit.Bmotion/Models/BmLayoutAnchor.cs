namespace Bit.Bmotion;

/// <summary>
/// The point of an element a layout (FLIP) animation is projected from - motion.dev's
/// <c>layoutAnchor</c>. Both components are fractions of the element's own box: <c>0</c> is its
/// left/top edge, <c>1</c> its right/bottom edge, <c>0.5</c> its centre.
/// <para>
/// It decides which part of the element appears to stay still while the box resizes. The default
/// <see cref="TopLeft"/> keeps the top-left corner pinned, which is right for text and list rows;
/// <see cref="Center"/> makes a growing box expand outward evenly; <c>new BmLayoutAnchor(1, 0)</c>
/// pins the top-right corner, which is what a right-aligned badge wants.
/// </para>
/// </summary>
/// <param name="X">Horizontal anchor, 0 (left) to 1 (right).</param>
/// <param name="Y">Vertical anchor, 0 (top) to 1 (bottom).</param>
public readonly record struct BmLayoutAnchor(double X, double Y)
{
    /// <summary>Pins the top-left corner - the default, and the safe choice for text.</summary>
    public static BmLayoutAnchor TopLeft => new(0, 0);

    /// <summary>Pins the centre, so a resizing box grows and shrinks evenly around it.</summary>
    public static BmLayoutAnchor Center => new(0.5, 0.5);

    /// <summary>Pins the bottom-right corner.</summary>
    public static BmLayoutAnchor BottomRight => new(1, 1);

    /// <summary>
    /// The anchor with both components clamped into <c>[0, 1]</c> and any non-finite component
    /// replaced by 0, so the projection maths can never receive NaN from a consumer.
    /// </summary>
    internal BmLayoutAnchor Sanitized() => new(Clamp(X), Clamp(Y));

    private static double Clamp(double v) => !double.IsFinite(v) ? 0 : v < 0 ? 0 : v > 1 ? 1 : v;
}
