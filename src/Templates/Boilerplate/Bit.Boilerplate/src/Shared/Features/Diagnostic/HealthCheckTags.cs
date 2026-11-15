namespace Boilerplate.Shared.Features.Diagnostic;

/// <summary>
/// Health check tags with a meaning of their own in the health report (See <see cref="HealthReportEntryDto"/>); a
/// check's category has <see cref="HealthCheckCategories"/>.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// On the stand-in for a check whose settings are missing (See AddNotConfiguredCheck). /healthz reports it as the
    /// entry's <see cref="HealthReportEntryDto.IsNotConfigured"/>, so the Operations page and a monitor tell a setup gap
    /// from a failing dependency without reading the description.
    /// </summary>
    public const string NotConfigured = "notConfigured";
}
