namespace Bit.Bheadless;

/// <summary>
/// Render-fragment context exposed by <see cref="BitBhFocusTrap"/>. Allows consumers
/// to render content driven by the enabled state.
/// </summary>
public sealed record BitBhFocusTrapRenderContext
{
    /// <summary>Whether the focus trap is currently active.</summary>
    public required bool Enabled { get; init; }
}
