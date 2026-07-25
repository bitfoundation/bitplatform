namespace Bit.Brouter;

/// <summary>How a stale cached loader result is served (see <see cref="BrouterOptions.StaleReloadMode"/>).</summary>
public enum BrouterStaleReloadMode
{
    /// <summary>
    /// Render the stale data immediately and re-run the loaders in the background, re-rendering when
    /// fresh data arrives - classic stale-while-revalidate (TanStack Router's default).
    /// </summary>
    Background = 0,

    /// <summary>Treat stale entries as cache misses: the navigation waits for the loader to finish.</summary>
    Blocking = 1,
}
