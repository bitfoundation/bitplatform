namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhFocusTrap"/> to its descendants.
/// Carries the enabled state so child components can react to whether focus
/// trapping is currently active.
/// </summary>
public sealed class BitBhFocusTrapContext
{
    /// <summary>Whether the focus trap is currently active.</summary>
    public required bool Enabled { get; init; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="BitBhFocusTrap"/>. Allows consumers
/// to render content driven by the enabled state.
/// </summary>
public sealed record BitBhFocusTrapRenderContext
{
    /// <summary>Whether the focus trap is currently active.</summary>
    public required bool Enabled { get; init; }
}
