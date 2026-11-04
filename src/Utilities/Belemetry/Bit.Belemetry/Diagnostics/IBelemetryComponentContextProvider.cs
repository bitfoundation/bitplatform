namespace Bit.Belemetry;

/// <summary>
/// The integration point for a tool that knows the live component tree - BevTools does - to add hierarchy and render
/// facts about the components a capture involves. Belemetry itself never walks the renderer: without a provider a
/// capture names the component types the framework's diagnostics reported, and says the hierarchy is unavailable.
/// Register it as a scoped or singleton service.
/// </summary>
public interface IBelemetryComponentContextProvider
{
    /// <summary>The provider's name, reported in the manifest.</summary>
    string Name { get; }

    /// <summary>Describes the live components of the given types; an empty list when it knows none.</summary>
    ValueTask<IReadOnlyList<BelemetryComponentInfo>> DescribeAsync(BelemetryComponentContextRequest request, CancellationToken cancellationToken);
}
