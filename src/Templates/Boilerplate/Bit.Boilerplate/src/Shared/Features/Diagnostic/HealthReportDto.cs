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

    /// <summary>
    /// How long the check took. A healthy result that is reused (See <see cref="VerifiedAt"/>) carries its own run's
    /// duration, not the cache lookup's.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// When the dependency was last really checked, for a check whose healthy result is reused for
    /// <see cref="CacheDuration"/> so probes don't each become a paid call (See AddCachedCheck). It can be well before
    /// the report. Null for a check that runs on every report.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>How long a healthy result of the check is reused. Null for a check that runs on every report.</summary>
    public TimeSpan? CacheDuration { get; set; }

    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// The whole exception, inner ones included. It can carry a connection string, hence the feature this report needs,
    /// and an OAuth token always gets null here: only the app's own session reads it.
    /// </summary>
    public string? Exception { get; set; }

    public string[] Tags { get; set; } = [];

    /// <summary>
    /// What kind of dependency the check watches, such as "Platform" or "Integrations": its tag that starts with
    /// <see cref="HealthCheckCategories.TagPrefix"/>, without the prefix (the tag stays in <see cref="Tags"/> too).
    /// Null for a check registered without one.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// What a page calls the check, such as "Database" for <c>AppDbContext</c>, in the app's own words (See
    /// HealthCheckDescriptions); the registration name for a check described nowhere.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// The check's icon, a bit BlazorUI <c>BitIconName</c>: its own, or its category's (See <see cref="HealthCheckCategories.IconOf"/>).
    /// </summary>
    public string IconName { get; set; } = "";

    /// <summary>What a pass proves, such as "Opens a connection to the app's database". Null when the app says nothing.</summary>
    public string? About { get; set; }

    /// <summary>
    /// The check stands in for a dependency whose settings are missing (See AddNotConfiguredCheck). It reports Degraded
    /// like a slow dependency does, but asks for another fix: configure the dependency, or remove the code that uses it.
    /// Set from the registration's <see cref="HealthCheckTags.NotConfigured"/> tag.
    /// </summary>
    public bool IsNotConfigured { get; set; }

    /// <summary>
    /// What the check put in its result, as text. A reused result's own run is in it too, under the names app versions
    /// from before <see cref="Duration"/>, <see cref="VerifiedAt"/> and <see cref="CacheDuration"/> read it by (See
    /// <see cref="LegacyDataKeys"/>).
    /// </summary>
    public Dictionary<string, string?> Data { get; set; } = [];

    /// <summary>
    /// The name <see cref="VerifiedAt"/> has in <see cref="Data"/> for app versions from before it was a field.
    /// </summary>
    public const string LegacyVerifiedAtKey = "CheckedAt";

    /// <summary>
    /// The keys of <see cref="Data"/> that repeat a reused result's run for the Operations page of app versions in users'
    /// hands from before <see cref="Duration"/>, <see cref="VerifiedAt"/> and <see cref="CacheDuration"/> were fields.
    /// Read the fields; the keys can go once <c>SupportedAppVersions</c> leaves those versions out.
    /// </summary>
    public static readonly string[] LegacyDataKeys = [nameof(Duration), nameof(CacheDuration), LegacyVerifiedAtKey];
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
