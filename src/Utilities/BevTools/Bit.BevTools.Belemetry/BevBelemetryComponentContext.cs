using Bit.Belemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// BevTools' component tree for Belemetry's captures: for each component type a capture involves, the live instance's
/// ancestry (component types from the root down) and render count - facts BevTools observed, never parameter values or
/// fields. When BevTools is disabled in the session, it knows nothing and says nothing.
/// </summary>
public sealed class BevBelemetryComponentContext(IServiceProvider services) : IBelemetryComponentContextProvider
{
    public string Name => "BevTools";

    public ValueTask<IReadOnlyList<BelemetryComponentInfo>> DescribeAsync(BelemetryComponentContextRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = services.GetService<BevToolsSession>();
        if (session is null || session.IsEnabled is false || session.IsDisposed) return ValueTask.FromResult<IReadOnlyList<BelemetryComponentInfo>>([]);

        // The hierarchy is resolved lazily; a capture is rare enough to pay for resolving it now.
        session.Components.Refresh(force: true);

        var described = new List<BelemetryComponentInfo>();
        foreach (var type in request.ComponentTypes.Take(Math.Max(1, request.MaxComponents)))
        {
            var instances = Find(session, type);
            if (instances.Count == 0) continue;

            // The most recently rendered instance stands for its type: a failure is about what is on screen now.
            var record = instances.OrderByDescending(r => r.LastRenderTicks).First();
            described.Add(new BelemetryComponentInfo
            {
                Type = type,
                Ancestors = Ancestors(record),
                RenderCount = instances.Sum(r => r.RenderCount),
                RenderMilliseconds = instances.Sum(r => r.TotalRenderMs),
                Association = "provider",
                Source = Name,
            });
        }

        return ValueTask.FromResult<IReadOnlyList<BelemetryComponentInfo>>(described);
    }

    /// <summary>The live, application instances of a component type (by its full name, as Belemetry names it).</summary>
    internal static List<BevComponentRecord> Find(BevToolsSession session, string typeName) =>
        session.Components.All.Where(r => r.IsDisposed is false && r.IsBevTools is false && string.Equals(r.Type.FullName, typeName, StringComparison.Ordinal)).ToList();

    private static List<string> Ancestors(BevComponentRecord record)
    {
        var ancestors = new List<string>();
        for (var parent = record.Parent; parent is not null && ancestors.Count < 32; parent = parent.Parent)
        {
            if (parent.IsBevTools is false) ancestors.Add(parent.Type.FullName ?? parent.TypeName);
        }
        ancestors.Reverse();
        return ancestors;
    }
}
