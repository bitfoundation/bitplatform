namespace Bit.Blow;

/// <summary>
/// Whether the flow animates: viewport transitions that ease rather than jump, and the marching dash of animated
/// edges. Motion helps most people keep their bearings and makes some people unwell, which is what the operating
/// system's "reduce motion" setting is for.
/// </summary>
public enum BlowReducedMotion
{
    /// <summary>Follow the operating system's <c>prefers-reduced-motion</c> setting (the default).</summary>
    System,

    /// <summary>
    /// Never animate: viewport transitions jump straight to where they were going, and animated edges keep their
    /// dash but stop moving.
    /// </summary>
    Always,

    /// <summary>Always animate, whatever the operating system says.</summary>
    Never,
}
