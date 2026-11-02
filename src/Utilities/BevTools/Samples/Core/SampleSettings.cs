namespace Bit.BevTools.Samples.Core;

/// <summary>What differs between the hosts the shared pages run in.</summary>
public sealed class SampleSettings
{
    /// <summary>
    /// The address the Network page requests through <see cref="System.Net.Http.IHttpClientFactory"/>, relative to the
    /// app's base address. The web hosts map <c>/api/weather</c>; a Blazor Hybrid app has no server of its own, so the
    /// MAUI sample points it elsewhere.
    /// </summary>
    public string HttpUrl { get; set; } = "api/weather";
}
