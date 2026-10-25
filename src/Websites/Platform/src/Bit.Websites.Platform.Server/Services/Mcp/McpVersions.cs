namespace Bit.Websites.Platform.Server.Services.Mcp;

/// <summary>
/// One release this endpoint answers for: the worktree of its tag, the codebase-memory project indexed from
/// it, and the documentation servers running out of it, keyed by the upstream names <see cref="McpProxyService"/> uses.
/// </summary>
public sealed record McpVersion(Version Number, string Tag, string WorktreePath, string CodebaseMemoryProject, IReadOnlyDictionary<string, Uri> DocumentationEndpoints)
{
    /// <summary>The three part version a caller passes as ?v=, such as 10.6.2.</summary>
    public string Name { get; } = Number.ToString(3);
}

/// <summary>
/// The versions <see cref="McpVersionsService"/> has finished preparing, and the ?v= a caller sends resolved
/// against them. Empty until the first one is ready, and on hosts that serve no versions at all.
/// </summary>
public sealed class McpVersions
{
    // One record, so a reader never sees a new generation next to the previous set.
    private Snapshot snapshot = new([], 0);

    public IReadOnlyList<McpVersion> Served => snapshot.Served;

    /// <summary>
    /// Changes on every change to <see cref="Served"/>, so the proxy drops tool lists it cached against an
    /// older set - a version that was not ready a minute ago, or one whose sites moved ports after a restart.
    /// </summary>
    public int Generation => snapshot.Generation;

    public McpVersion? Latest => snapshot.Served.FirstOrDefault();

    /// <summary>A version that is not served, and a caller that names none, both get the newest one.</summary>
    public McpVersion? Resolve(string? requested) => Resolve(snapshot.Served, requested);

    /// <summary>
    /// Names the release that answers a caller, for the server instructions: the only way a caller learns that the
    /// ?v= it sent is not served and an older release answers instead - a project created from a prerelease asks for
    /// a version that is only served once it is released. Null while no release is ready.
    /// </summary>
    public string? Describe(string? requested)
    {
        var served = snapshot.Served;
        var version = Resolve(served, requested);

        if (version is null) return null;

        if (string.IsNullOrWhiteSpace(requested))
            return $"Its bit platform tools answer for release {version.Name}, the newest one, as this connection names no release (the v query of this endpoint).";

        if (version.Name == requested.Trim())
            return $"Its bit platform tools answer for release {version.Name}, the one this connection asked for.";

        return $"This connection asked for release {requested.Trim()}, which this endpoint does not serve (it is not released yet, or not ready here), so its bit platform tools answer for {version.Name}, the newest release served: an API added after {version.Name} is missing from their answers rather than absent from the library.";
    }

    private static McpVersion? Resolve(McpVersion[] served, string? requested)
        => (string.IsNullOrWhiteSpace(requested) ? null : served.FirstOrDefault(version => version.Name == requested.Trim())) ?? served.FirstOrDefault();

    internal void Publish(IEnumerable<McpVersion> versions)
        => snapshot = new([.. versions.OrderByDescending(version => version.Number)], snapshot.Generation + 1);

    private sealed record Snapshot(McpVersion[] Served, int Generation);
}
