namespace Bit.Bheadless;

/// <summary>
/// Cascading context shared by <see cref="BitBhAvatar"/> with its
/// <see cref="BitBhAvatarImage"/> and <see cref="BitBhAvatarFallback"/> children.
/// Carries the current image loading status and the setter the image uses to
/// report transitions, so the fallback can coordinate without prop-drilling.
/// </summary>
public sealed class BitBhAvatarContext
{
    /// <summary>The current image loading status.</summary>
    public required BitBhAvatarImageLoadingStatus Status { get; init; }

    /// <summary>
    /// Called by <see cref="BitBhAvatarImage"/> to report a loading-status change.
    /// Updates the root's state and re-renders the avatar subtree.
    /// </summary>
    public required Action<BitBhAvatarImageLoadingStatus> SetStatus { get; init; }
}
