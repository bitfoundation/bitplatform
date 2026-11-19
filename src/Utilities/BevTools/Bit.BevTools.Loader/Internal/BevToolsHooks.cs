namespace Bit.BevTools;

/// <summary>The process-wide hook points Bit.BevTools fills once it is registered or attached.</summary>
internal static class BevToolsHooks
{
    private static IBevToolsHttpRecorder? _http;

    public static IBevToolsHttpRecorder? Http
    {
        get => Volatile.Read(ref _http);
        set => Volatile.Write(ref _http, value);
    }
}
