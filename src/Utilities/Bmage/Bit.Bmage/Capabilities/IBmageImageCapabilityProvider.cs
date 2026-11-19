namespace Bit.Bmage;

/// <summary>Supplies <see cref="BmageImageCapabilities"/>. The Blazor package provides a browser-backed implementation.</summary>
public interface IBmageImageCapabilityProvider
{
    /// <summary>Detects capabilities, caching the result. Safe to call repeatedly.</summary>
    ValueTask<BmageImageCapabilities> GetAsync(CancellationToken cancellationToken = default);
}
