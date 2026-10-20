namespace Bit.Bonboarding;

/// <summary>A measurement pushed from the browser.</summary>
public sealed record BonboardingStepGeometry
{
    /// <summary>A geometry meaning "there is no target and none is expected".</summary>
    public static BonboardingStepGeometry Missing { get; } = new();

    /// <summary>True when at least one target element was located.</summary>
    public bool Found { get; init; }

    /// <summary>Union of the target rectangles in viewport coordinates.</summary>
    public BonboardingRect Target { get; init; }

    /// <summary>The visual viewport.</summary>
    public BonboardingRect Viewport { get; init; }

    /// <summary>Measured popover size; zero until the popover has rendered.</summary>
    public BonboardingSize Popover { get; init; }

    /// <summary>False when the target is scrolled out of sight or hidden.</summary>
    public bool TargetVisible { get; init; }

    /// <summary>Monotonic counter, used to drop out-of-order frames.</summary>
    public long Sequence { get; init; }

    /// <summary>The <see cref="BonboardingStepActivation.Activation"/> this frame was measured for.</summary>
    public long Activation { get; init; }

    /// <summary>
    /// The popover's measured corner radius in pixels, so the arrow stays clear of the corners
    /// whatever the theme sets. <see langword="null"/> when unknown.
    /// </summary>
    public double? PopoverRadius { get; init; }
}
