namespace Bit.Brouter;

/// <summary>
/// When a <see cref="BrouterLink"/> preloads its destination's loader data into the router's cache
/// (see <see cref="BrouterLink.Preload"/> / <see cref="BrouterOptions.DefaultLinkPreload"/>).
/// </summary>
public enum BrouterLinkPreload
{
    /// <summary>No preloading (default).</summary>
    None = 0,

    /// <summary>
    /// Preload on interaction intent: pointer hover / touchstart / keyboard focus, debounced by
    /// <see cref="BrouterOptions.PreloadDelay"/> so brushing past a link doesn't fetch.
    /// </summary>
    Intent = 1,

    /// <summary>Preload when the link scrolls into the viewport (IntersectionObserver), once.</summary>
    Viewport = 2,

    /// <summary>Preload as soon as the link renders.</summary>
    Render = 3,
}
