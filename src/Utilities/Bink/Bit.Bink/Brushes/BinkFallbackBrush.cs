
namespace Bit.Bink;

/// <summary>
/// Stands in for a brush kind this build has no registration for, such as one from a newer version or an
/// application's custom brush. It draws like a pen, so the stroke still looks approximately right, but it reports
/// and writes back the kind it was read with. Falling back to <see cref="BinkPenBrush"/> itself lost the kind on
/// every save, and a collaborator without the registration who edited such a stroke sent it to everyone, including
/// participants that have the brush, as a pen.
/// </summary>
internal sealed record BinkFallbackBrush : BinkBrush
{
    /// <summary>Creates a stand-in for <paramref name="kind"/> with the pen's width and pressure response.</summary>
    /// <remarks>
    /// Only the width and the curve are set. Every other property keeps <see cref="BinkBrush"/>'s own default, which
    /// is what a reader assumes for a field the file leaves out; the serializer relies on that.
    /// </remarks>
    public BinkFallbackBrush(string kind)
    {
        StoredKind = kind;
        Width = 4f;
        PressureCurve = BinkPressureCurve.Pen;
    }

    /// <summary>The kind the stroke was stored with.</summary>
    public string StoredKind { get; }

    /// <summary>The kind's own settings as they were stored, written back unchanged.</summary>
    public IReadOnlyDictionary<string, float>? StoredParameters { get; init; }

    /// <inheritdoc />
    public override string Kind => StoredKind;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, float>? GetParameters() => StoredParameters;

    /// <inheritdoc />
    public override BinkBrush WithParameters(IReadOnlyDictionary<string, float> parameters)
        => this with { StoredParameters = new Dictionary<string, float>(parameters, StringComparer.Ordinal) };

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkVariableWidthBrushes.Build(points, builder, this, roundCaps: true);
}
