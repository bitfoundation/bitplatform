
namespace Bit.Bink;

/// <summary>
/// Describes how strokes are captured and rendered. A brush is an immutable value: use <c>with</c> expressions to
/// derive variants (<c>brush with { Color = BinkColor.Parse("#c00") }</c>).
/// </summary>
/// <remarks>
/// <para>
/// A brush has two responsibilities: it configures input processing (<see cref="Smoothing"/>, <see cref="Spacing"/>,
/// <see cref="SimulatePressure"/>) and it turns stored points into vector geometry (<see cref="BuildGeometry(BinkStrokePointsView, BinkPathBuilder)"/>).
/// Geometry is a pure function of the points and the brush, which is what makes canvas rendering, SVG export and
/// PNG export produce the same result.
/// </para>
/// <para>
/// To add a brush, derive from this record, give it a unique <see cref="Kind"/>, implement <see cref="BuildGeometry(BinkStrokePointsView, BinkPathBuilder)"/>
/// and register the kind with <see cref="BinkBrushRegistry"/> so documents containing it can be deserialized.
/// </para>
/// </remarks>
public abstract record BinkBrush
{
    /// <summary>Stable discriminator used in serialized documents (e.g. <c>"pen"</c>).</summary>
    public abstract string Kind { get; }

    /// <summary>Ink color.</summary>
    public BinkColor Color { get; init; } = BinkColor.Black;

    /// <summary>Maximum stroke width in document units (reached at full pressure).</summary>
    public float Width { get; init; } = 4f;

    /// <summary>Opacity 0..1 applied to the whole stroke as one unit (self-overlaps do not darken).</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>Blend mode used when compositing the stroke onto its layer.</summary>
    public BinkBlendMode BlendMode { get; init; } = BinkBlendMode.Normal;

    /// <summary>Mapping from pressure to width factor.</summary>
    public BinkPressureCurve PressureCurve { get; init; } = BinkPressureCurve.Linear;

    /// <summary>
    /// Input smoothing 0..1 ("streamline"). Each new point is pulled toward the previous one by this amount, which
    /// removes jitter at the cost of slightly rounding fast corners. 0 disables smoothing.
    /// </summary>
    public float Smoothing { get; init; } = 0.5f;

    /// <summary>
    /// Minimum spacing between stored points as a fraction of <see cref="Width"/>. Closer samples are merged.
    /// Higher values give smaller documents and smoother lines; lower values keep more detail.
    /// </summary>
    public float Spacing { get; init; } = 0.25f;

    /// <summary>
    /// When the device reports no real pressure (mouse, most touch screens), derive pressure from drawing speed so
    /// strokes still thin out on fast movements. Ignored for pens with pressure.
    /// </summary>
    public bool SimulatePressure { get; init; } = true;

    /// <summary>Length in document units over which the stroke start tapers in (0 = no taper).</summary>
    public float TaperStart { get; init; }

    /// <summary>Length in document units over which the stroke end tapers out (0 = no taper).</summary>
    public float TaperEnd { get; init; }

    /// <summary>Builds the vector geometry for a set of points using this brush.</summary>
    /// <param name="points">Processed points in document units.</param>
    /// <param name="builder">A reusable path builder; it is reset before use.</param>
    public abstract BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder);

    /// <summary>Builds geometry with a fresh builder.</summary>
    public BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points) => BuildGeometry(points, new BinkPathBuilder(Math.Max(points.Count * 2, 16)));

    /// <summary>The width to use when hit testing strokes drawn with this brush.</summary>
    public virtual float HitTestWidth => MathF.Max(Width, 1f);

    /// <summary>
    /// Conservative bounds of a stroke drawn with this brush, computed from the points' bounds alone. Used before
    /// the exact outline exists, so it must never be smaller than the real geometry: it accounts for the widest
    /// the pressure curve can go and for the miter allowance the outliner may add at corners.
    /// </summary>
    public virtual BinkRect GetBounds(BinkRect pointBounds)
    {
        const float miterAllowance = 1.6f;
        float maxFactor = MathF.Max(PressureCurve.MaxFactor, PressureCurve.MinFactor);
        return pointBounds.Inflate(Width / 2f * MathF.Max(maxFactor, 1f) * miterAllowance + 1f);
    }

    /// <summary>Validates the brush values are usable; throws otherwise.</summary>
    public virtual void Validate()
    {
        if (!(Width > 0) || !float.IsFinite(Width)) throw new InvalidOperationException($"Brush width must be positive (was {Width}).");
        if (!float.IsFinite(Opacity) || Opacity < 0 || Opacity > 1) throw new InvalidOperationException($"Brush opacity must be between 0 and 1 (was {Opacity}).");
    }
}
