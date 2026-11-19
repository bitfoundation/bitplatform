
namespace Bit.BevTools;

/// <summary>Describes an additional panel. <see cref="ComponentType"/> must be a Blazor component; it receives no parameters and can inject BevTools services.</summary>
public sealed record BevToolsPanelDescriptor(string Id, string Title, Type ComponentType, int Order = 100)
{
    /// <summary>Optional platform restriction; when set the panel is only shown when the renderer name matches (e.g. "Server").</summary>
    public string? RequiredPlatform { get; init; }
}
