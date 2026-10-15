namespace Bit.Belemetry.Samples.Core;

/// <summary>What differs between the hosts the shared pages run in.</summary>
public sealed class SampleSettings
{
    /// <summary>
    /// The URL the "fetch" button requests. The web hosts serve <c>api/weather</c>; the MAUI app has no server, so it
    /// fetches a file of its own <c>wwwroot</c>.
    /// </summary>
    public string FetchUrl { get; set; } = "api/weather";
}
