namespace Bit.Bmotion.Models;

/// <summary>
/// Options for the drag gesture on a Motion element.
/// </summary>
public class DragOptions
{
    /// <summary>Restrict drag to a single axis. Null = both axes.</summary>
    public DragAxis Axis { get; set; } = DragAxis.Both;

    /// <summary>
    /// Constraint bounds (in px relative to the element's resting position).
    /// Null = unconstrained.
    /// </summary>
    public DragConstraints? Constraints { get; set; }

    /// <summary>
    /// Elasticity when the drag exceeds constraints (0 = rigid, 1 = fully elastic).
    /// Default: 0.35.
    /// </summary>
    public double Elastic { get; set; } = 0.35;

    /// <summary>
    /// Whether to apply momentum / inertia after releasing. Default: true.
    /// </summary>
    public bool Momentum { get; set; } = true;

    /// <summary>
    /// Transition applied when snapping back to constraints after release.
    /// Defaults to a spring.
    /// </summary>
    public TransitionConfig? SnapTransition { get; set; }

    /// <summary>
    /// If true, the draggable element will spring back to its center (origin) when released.
    /// Default: false.
    /// </summary>
    public bool SnapToOrigin { get; set; }

    /// <summary>
    /// Locks drag to the dominant movement axis once detected.
    /// For example, moving mostly horizontally will lock drag to x only.
    /// Default: false.
    /// </summary>
    public bool DirectionLock { get; set; }

    internal object ToJsObject()
    {
        var d = new Dictionary<string, object?>
        {
            ["drag"] = true,
            ["dragAxis"] = Axis == DragAxis.Both ? null : Axis.ToString().ToLowerInvariant(),
            ["dragElastic"] = Elastic,
            ["dragMomentum"] = Momentum,
        };

        if (Constraints != null)
            d["dragConstraints"] = Constraints.ToJsObject();

        if (SnapTransition != null)
            d["dragSnapTransition"] = SnapTransition.ToJsObject();

        if (SnapToOrigin) d["dragSnapToOrigin"] = true;
        if (DirectionLock) d["dragDirectionLock"] = true;

        return d;
    }
}

public class DragConstraints
{
    public double? Left { get; set; }
    public double? Right { get; set; }
    public double? Top { get; set; }
    public double? Bottom { get; set; }

    public static DragConstraints Horizontal(double left, double right)
        => new() { Left = left, Right = right };

    public static DragConstraints Vertical(double top, double bottom)
        => new() { Top = top, Bottom = bottom };

    public static DragConstraints Box(double left, double right, double top, double bottom)
        => new() { Left = left, Right = right, Top = top, Bottom = bottom };

    internal object ToJsObject()
    {
        var d = new Dictionary<string, object?>();
        if (Left.HasValue)   d["left"]   = Left.Value;
        if (Right.HasValue)  d["right"]  = Right.Value;
        if (Top.HasValue)    d["top"]    = Top.Value;
        if (Bottom.HasValue) d["bottom"] = Bottom.Value;
        return d;
    }
}

public enum DragAxis { Both, X, Y }
