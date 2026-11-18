namespace Bit.Bmage;

/// <summary>Reports the fixed capabilities of managed .NET with no browser present.</summary>
public sealed class BmageManagedCapabilityProvider : IBmageImageCapabilityProvider
{
    public static BmageManagedCapabilityProvider Instance { get; } = new();

    public ValueTask<BmageImageCapabilities> GetAsync(CancellationToken cancellationToken = default) => new(BmageImageCapabilities.Managed);
}
