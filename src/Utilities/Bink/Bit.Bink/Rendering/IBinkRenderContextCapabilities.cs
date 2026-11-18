namespace Bit.Bink;

/// <summary>
/// Optional capability of a render context: whether it can accept a cached representation for repeated paths. Hosts may
/// use it to skip regenerating geometry for unchanged elements.
/// </summary>
public interface IBinkRenderContextCapabilities
{
    /// <summary>The context renders text with real fonts. When false, text elements draw a placeholder outline only.</summary>
    bool SupportsText { get; }

    /// <summary>The context can decode and draw images. When false, image elements draw a placeholder outline only.</summary>
    bool SupportsImages { get; }
}
