namespace Boilerplate.Shared.Features.Diagnostic;

/// <summary>
/// The body of /healthz (See MapAppHealthChecks). It keeps the HealthChecks UI format's names and adds what the
/// health checks page needs on top: when the report was made, and each check's failure status and timeout. And which
/// build answered, on which instance, since when: a monitor that polls this marks a deployment where
/// <see cref="ApplicationVersion"/> changes, and a restart where it stays and <see cref="StartedAt"/> moves.
/// </summary>
public class HealthReportDto
{
    public HealthCheckStatus Status { get; set; }

    public TimeSpan TotalDuration { get; set; }

    public DateTimeOffset CheckedAt { get; set; }

    /// <summary>
    /// The entry assembly's informational version. A Release build carries its commit after a '+'
    /// (IncludeSourceRevisionInInformationalVersion, see Directory.Build.props).
    /// </summary>
    public string? ApplicationVersion { get; set; }

    /// <summary>The instance that answered: under scale-out each one reports for itself.</summary>
    public string? InstanceName { get; set; }

    /// <summary>When the answering process started, in UTC.</summary>
    public DateTimeOffset StartedAt { get; set; }

    public Dictionary<string, HealthReportEntryDto> Entries { get; set; } = [];
}

public class HealthReportEntryDto
{
    public HealthCheckStatus Status { get; set; }

    /// <summary>
    /// What the check reports when it fails. Unhealthy makes /health answer 503, which takes the instance out of rotation.
    /// </summary>
    public HealthCheckStatus FailureStatus { get; set; }

    public string? Description { get; set; }

    public TimeSpan Duration { get; set; }

    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// The whole exception, inner ones included. It can carry a connection string, hence the feature this report needs,
    /// and an OAuth token always gets null here: only the app's own session reads it.
    /// </summary>
    public string? Exception { get; set; }

    public string[] Tags { get; set; } = [];

    public Dictionary<string, string?> Data { get; set; } = [];
}

/// <summary>
/// Mirrors Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus, which the client doesn't reference.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HealthCheckStatus>))]
public enum HealthCheckStatus
{
    Unhealthy = 0,
    Degraded = 1,
    Healthy = 2
}
