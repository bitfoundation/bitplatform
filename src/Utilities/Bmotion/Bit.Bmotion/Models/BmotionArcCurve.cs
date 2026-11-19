namespace Bit.Bmotion;

/// <summary>
/// A concrete quadratic Bézier an arc animation follows, plus how strongly the element turns to
/// face along it. Positions are in the element's transform space (px).
/// </summary>
internal readonly record struct BmotionArcCurve(
    double FromX, double FromY, double ControlX, double ControlY, double ToX, double ToY, double Rotate)
{
    /// <summary>The point at eased progress <paramref name="t"/> (0-1) along the curve.</summary>
    public (double X, double Y) PointAt(double t)
    {
        double u = 1 - t;
        double a = u * u, b = 2 * u * t, c = t * t;
        return (a * FromX + b * ControlX + c * ToX,
                a * FromY + b * ControlY + c * ToY);
    }

    /// <summary>
    /// The element's rotation in degrees at progress <paramref name="t"/>: the curve's tangent
    /// angle scaled by <see cref="Rotate"/>. Zero where the tangent is degenerate (a zero-length
    /// journey), so the element simply stays upright rather than snapping to an arbitrary angle.
    /// </summary>
    public double RotationAt(double t)
    {
        if (Rotate <= 0) return 0;
        // Derivative of a quadratic Bézier: 2(1-t)(C-P0) + 2t(P1-C).
        double u = 1 - t;
        double dx = 2 * u * (ControlX - FromX) + 2 * t * (ToX - ControlX);
        double dy = 2 * u * (ControlY - FromY) + 2 * t * (ToY - ControlY);
        if (Math.Abs(dx) < 1e-12 && Math.Abs(dy) < 1e-12) return 0;
        return Math.Atan2(dy, dx) * (180 / Math.PI) * Rotate;
    }
}
