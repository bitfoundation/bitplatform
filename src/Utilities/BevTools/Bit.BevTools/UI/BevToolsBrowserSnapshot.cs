namespace Bit.BevTools;

public sealed class BevToolsBrowserSnapshot
{
    public bool? Online { get; set; }

    public string? Visibility { get; set; }

    public string? UserAgent { get; set; }

    public double? JsHeapUsedMb { get; set; }

    public double? JsHeapLimitMb { get; set; }

    public double? NavigationLoadMs { get; set; }

    public double? DomContentLoadedMs { get; set; }

    public int? ResourceCount { get; set; }

    /// <summary>The id the page hub gave the runtime that attached.</summary>
    public string? TargetId { get; set; }

    /// <summary>Which timing entries the browser has; null from a hub that does not say.</summary>
    public BevToolsResponsivenessSupport? Responsiveness { get; set; }
}
