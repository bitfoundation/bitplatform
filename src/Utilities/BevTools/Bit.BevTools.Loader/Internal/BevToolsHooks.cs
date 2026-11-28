namespace Bit.BevTools;

/// <summary>The process-wide hook points Bit.BevTools fills once it is registered or attached.</summary>
internal static class BevToolsHooks
{
    private static readonly AsyncLocal<bool> OwnWork = new();
    private static IBevToolsHttpRecorder? _http;

    public static IBevToolsHttpRecorder? Http
    {
        get => Volatile.Read(ref _http);
        set => Volatile.Write(ref _http, value);
    }

    /// <summary>
    /// True while the current async flow runs BevTools' own work. Kept here rather than in Bit.BevTools because both
    /// halves have to agree on it: the loader marks its <see cref="BevToolsLoaderOptions.Authorize"/> call and the load
    /// before Bit.BevTools exists, and what it captures before an attach must skip the same work Bit.BevTools skips after.
    /// </summary>
    public static bool IsOwnWork => OwnWork.Value;

    /// <summary>
    /// Marks the current async flow as BevTools' own work until disposed: application code BevTools calls into - an
    /// <c>AuthenticationStateProvider</c>, the <see cref="BevToolsLoaderOptions.Authorize"/> callback - can make
    /// requests, log and raise activities, and none of that is application activity.
    /// </summary>
    public static IDisposable BeginOwnWork()
    {
        var previous = OwnWork.Value;
        OwnWork.Value = true;
        return new OwnWorkScope(previous);
    }

    private sealed class OwnWorkScope(bool previous) : IDisposable
    {
        public void Dispose() => OwnWork.Value = previous;
    }
}
