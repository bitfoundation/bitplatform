namespace Bit.BevTools;

public interface IBevToolsStateProviderRegistry
{
    /// <summary>Registers a provider for the current BevTools session. Dispose the result to unregister.</summary>
    IDisposable Register(IBevToolsStateProvider provider);

    IReadOnlyList<IBevToolsStateProvider> Providers { get; }
}
