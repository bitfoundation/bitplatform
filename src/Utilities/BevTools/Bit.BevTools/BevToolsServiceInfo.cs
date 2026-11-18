using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

public sealed class BevToolsServiceInfo
{
    public required int Index { get; init; }

    public required Type ServiceType { get; init; }

    public required string ServiceTypeName { get; init; }

    public required ServiceLifetime Lifetime { get; init; }

    public Type? ImplementationType { get; init; }

    public required string ImplementationName { get; init; }

    /// <summary>"Type", "Factory" or "Instance".</summary>
    public required string RegistrationKind { get; init; }

    public bool IsKeyed { get; init; }

    public string? Namespace { get; init; }

    public bool IsFramework { get; init; }

    /// <summary>Constructor dependencies of the implementation type (only resolvable for type registrations).</summary>
    public List<BevToolsServiceInfo> Dependencies { get; } = [];

    public List<string> UnresolvedDependencies { get; } = [];

    public List<BevToolsServiceInfo> Consumers { get; } = [];

    /// <summary>True when this registration is shadowed by a later registration of the same service type.</summary>
    public bool IsShadowed { get; set; }

    public int SubtreeSize { get; set; }
}
