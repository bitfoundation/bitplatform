namespace Bit.Belemetry;

/// <summary>The release, environment and runtime a capture was made in.</summary>
public sealed class BelemetryReleaseInfo
{
    /// <summary><c>service.name</c>.</summary>
    public required string ServiceName { get; init; }

    /// <summary><c>service.version</c>: the release.</summary>
    public string? ServiceVersion { get; init; }

    /// <summary><c>service.namespace</c>.</summary>
    public string? ServiceNamespace { get; init; }

    /// <summary><c>deployment.environment.name</c>.</summary>
    public string? Environment { get; init; }

    /// <summary>The source commit (<see cref="BelemetryReleaseOptions.Commit"/>).</summary>
    public string? Commit { get; init; }

    /// <summary>The build id (<see cref="BelemetryReleaseOptions.BuildId"/>).</summary>
    public string? BuildId { get; init; }

    /// <summary>The configuration version (<see cref="BelemetryReleaseOptions.ConfigurationVersion"/>).</summary>
    public string? ConfigurationVersion { get; init; }

    /// <summary>The release metadata (<see cref="BelemetryReleaseOptions.Metadata"/>), sanitized.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>The host kind: WebAssembly, Server or Hybrid.</summary>
    public BelemetryHostKind Host { get; init; }

    /// <summary>The .NET runtime (<c>.NET 10.0.1</c>).</summary>
    public string? Runtime { get; init; }

    /// <summary>The operating system family the .NET side runs on (<c>Browser</c>, <c>Windows</c>, <c>Linux</c>, ...), never its full description.</summary>
    public string? OperatingSystem { get; init; }

    /// <summary>The process architecture.</summary>
    public string? Architecture { get; init; }

    /// <summary>The Belemetry version.</summary>
    public string? SdkVersion { get; init; }

    /// <summary>What the browser script reported about the browser (brands, platform, mobile, language), when it was reachable.</summary>
    public IReadOnlyDictionary<string, string> Browser { get; init; } = new Dictionary<string, string>();
}
