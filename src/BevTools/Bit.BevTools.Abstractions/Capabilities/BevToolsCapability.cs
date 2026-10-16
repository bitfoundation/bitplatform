namespace Bit.BevTools;

/// <summary>Honest availability classification for every piece of information BevTools shows.</summary>
public enum BevCapabilityStatus
{
    Available,
    Partial,
    RequiresInstrumentation,
    RequiresIntegration,
    RequiresRuntimeSupport,
    NotReliable,
    NotAvailable,
}

public sealed record BevToolsCapability(string Area, string Name, BevCapabilityStatus Status, string Explanation)
{
    /// <summary>Platforms where the capability applies ("Server", "WebAssembly", "Static", "WebView", "All").</summary>
    public string Platforms { get; init; } = "All";
}
