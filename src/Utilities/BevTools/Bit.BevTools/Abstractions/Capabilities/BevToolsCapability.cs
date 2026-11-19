namespace Bit.BevTools;

public sealed record BevToolsCapability(string Area, string Name, BevToolsCapabilityStatus Status, string Explanation)
{
    /// <summary>Platforms where the capability applies ("Server", "WebAssembly", "Static", "WebView", "All").</summary>
    public string Platforms { get; init; } = "All";
}
