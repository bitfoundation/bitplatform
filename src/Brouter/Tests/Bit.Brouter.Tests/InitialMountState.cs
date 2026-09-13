namespace Bit.Brouter.Tests;

/// <summary>
/// Shared observation state for <c>InitialMountTests</c> (static like <see cref="NavigationLockState"/>;
/// the test assembly runs without MSTest parallelization, so tests never race on it). Captures the
/// property under test: whether the routed content already existed at the FIRST dispatcher yield
/// after the mount began - i.e. whether the first render batch (the first frame the browser paints
/// after prerendered HTML is replaced) already contained the matched route, or the mount flashed an
/// empty router first.
/// </summary>
public static class InitialMountState
{
    /// <summary>Live instances of <c>InitialMountContent</c> (incremented in its OnInitialized).</summary>
    public static int ContentInstances;

    /// <summary>
    /// Recorded by <c>InitialBatchProbe</c>: was the routed content already initialized when the
    /// first posted continuation ran? Null until the probe has recorded.
    /// </summary>
    public static bool? ContentPresentAtFirstYield;

    public static void Reset()
    {
        ContentInstances = 0;
        ContentPresentAtFirstYield = null;
    }
}
