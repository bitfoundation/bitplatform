namespace Bit.BevTools;

/// <summary>The route the session is on: from the <c>RouteView</c> (or <c>AuthorizeRouteView</c>) the router rendered.</summary>
public sealed record BevToolsCurrentRoute(
    string? Uri,
    string? PageType,
    string? PageDisplayName,
    long? PageInstanceId,
    string? Template,
    bool TemplateInferred,
    IReadOnlyList<KeyValuePair<string, string>> RouteValues,
    IReadOnlyList<KeyValuePair<string, string>> Query,
    string? Layout,
    string Source);
