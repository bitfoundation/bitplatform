namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Cascading context provided by <see cref="BitBhPortalGroup"/> to nested
/// <see cref="BitBhPortal"/> components. Carries the target group ID
/// where portalled content should be rendered.
/// </summary>
public sealed class BitBhPortalContext
{
    /// <summary>
    /// The group ID where portalled content should be rendered.
    /// When null, portals render into the default <see cref="BitBhPortalOutlet"/>.
    /// </summary>
    public string? TargetId { get; init; }
}
