namespace Bit.Bmotion;
/// <summary>
/// Options for the drag gesture on a Bmotion element.
/// </summary>
public class BmotionDragOptions
{
    /// <summary>Restrict drag to a single axis. Defaults to <see cref="BmotionDragAxis.Both"/>.</summary>
    public BmotionDragAxis Axis { get; set; } = BmotionDragAxis.Both;

    /// <summary>
    /// Constraint bounds (in px relative to the element's resting position).
    /// Null = unconstrained.
    /// </summary>
    public BmotionDragConstraints? Constraints { get; set; }

    /// <summary>
    /// Elasticity when the drag exceeds constraints (0 = rigid, 1 = fully elastic).
    /// Default: 0.35. Values are clamped to the [0, 1] range.
    /// </summary>
    public double Elastic
    {
        get => _elastic;
        // Reject NaN/±Infinity (Math.Clamp passes NaN straight through), which would otherwise
        // destabilise the drag elasticity math; fall back to the default when not finite.
        set => _elastic = double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0.35;
    }
    private double _elastic = 0.35;

    /// <summary>
    /// Whether to apply momentum / inertia after releasing. Default: true.
    /// </summary>
    public bool Momentum { get; set; } = true;

    /// <summary>
    /// Transition applied when snapping back to constraints after release.
    /// Defaults to a spring.
    /// </summary>
    public BmotionTransitionConfig? SnapTransition { get; set; }

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
}
