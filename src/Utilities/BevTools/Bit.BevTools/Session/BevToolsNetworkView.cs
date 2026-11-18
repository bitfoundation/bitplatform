namespace Bit.BevTools;

internal sealed class BevToolsNetworkView
{
    /// <summary>One row per endpoint instead of one per request.</summary>
    public bool ByEndpoint { get; set; }

    /// <summary>Only the requests to this <see cref="BevToolsHttpRecord.EndpointTemplate"/>.</summary>
    public string? Endpoint { get; set; }

    public BevToolsSortState Requests { get; } = new("time", ascending: false);

    public BevToolsSortState Endpoints { get; } = new("requests", ascending: false);
}
