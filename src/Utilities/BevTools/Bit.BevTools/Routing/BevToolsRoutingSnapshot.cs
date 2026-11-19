namespace Bit.BevTools;

/// <summary>What the Routing panel shows: the current route, every route the router knows, and how both were read.</summary>
public sealed record BevToolsRoutingSnapshot(BevToolsCurrentRoute Current, IReadOnlyList<BevToolsRouteEntry> Routes, IReadOnlyList<string> RouteAssemblies, string RoutesSource);
