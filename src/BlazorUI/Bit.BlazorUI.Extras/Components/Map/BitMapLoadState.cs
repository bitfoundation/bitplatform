namespace Bit.BlazorUI;

/// <summary>
/// Lifecycle state of a <see cref="BitMap{TMapProvider}"/>, surfaced through
/// <see cref="BitMap{TMapProvider}.LoadState"/> and
/// <see cref="BitMap{TMapProvider}.OnLoadStateChanged"/>.
/// <para>
/// The states are deliberately distinct rather than a single "has the map loaded" flag:
/// a map that never initialized because the browser has no WebGL needs a different message
/// (and a different fix) than one whose vendor script failed to download.
/// </para>
/// </summary>
public enum BitMapLoadState
{
    /// <summary>The component has rendered but initialization has not started yet - it is still prerendering, or waiting to scroll into view under <see cref="BitMap{TMapProvider}.LazyLoad"/>.</summary>
    Idle,

    /// <summary>The provider's scripts/stylesheets are downloading, or the map instance is being created.</summary>
    Loading,

    /// <summary>The map is live and every imperative method can be called.</summary>
    Ready,

    /// <summary>Initialization failed. The exception is carried by <see cref="BitMap{TMapProvider}.LoadError"/>.</summary>
    Failed,

    /// <summary>The browser cannot run this provider at all - currently only raised when a WebGL-backed provider is used without WebGL support.</summary>
    Unsupported,
}
