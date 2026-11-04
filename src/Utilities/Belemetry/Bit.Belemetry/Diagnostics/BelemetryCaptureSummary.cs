using System.Globalization;

namespace Bit.Belemetry;

/// <summary>The preview's view of a capture: what each section holds, and what evidence is missing and why.</summary>
internal static class BelemetryCaptureSummary
{
    public static List<BelemetryBundleSection> Sections(BelemetryDiagnosticCapture capture, BelemetryCaptureContent content)
    {
        var events = content.Events;
        var dotnet = events.Count(e => e.Source != BelemetryDiagnosticSource.Browser);
        var browser = events.Count - dotnet;
        var exportedDotnet = capture.Includes(BelemetryBundleSections.Timeline) ? events.Count(e => e.Source != BelemetryDiagnosticSource.Browser && capture.IsExported(e)) : 0;
        var exportedBrowser = capture.Includes(BelemetryBundleSections.Browser) ? events.Count(e => e.Source == BelemetryDiagnosticSource.Browser && capture.IsExported(e)) : 0;

        return
        [
            Section(capture, BelemetryBundleSections.Release, content.Release is null ? 0 : 1, 0, null),
            Section(capture, BelemetryBundleSections.Exceptions, content.Groups.Count, 0, null),
            Section(capture, BelemetryBundleSections.Timeline, exportedDotnet, dotnet - exportedDotnet + content.LimitOmitted + content.ConsentOmitted + content.LookbackOmitted,
                    content.LimitOmitted > 0 ? $"{content.LimitOmitted} older events left out at the bundle size limit" : null),
            content.BrowserRequested is false
                ? new BelemetryBundleSection(BelemetryBundleSections.Browser, "excluded", 0, 0, "not requested")
                : content.BrowserAvailable is false
                    ? new BelemetryBundleSection(BelemetryBundleSections.Browser, "unavailable", 0, 0, "the browser script was not reachable")
                    : Section(capture, BelemetryBundleSections.Browser, exportedBrowser, browser - exportedBrowser, null),
            Section(capture, BelemetryBundleSections.Components, content.Components.Count, 0,
                    content.ComponentProvider is null ? "component hierarchy unavailable (no IBelemetryComponentContextProvider)" : null),
            content.SnapshotsRequested is false && content.Snapshots.Count == 0
                ? new BelemetryBundleSection(BelemetryBundleSections.Snapshots, "excluded", 0, 0, "not requested")
                : Section(capture, BelemetryBundleSections.Snapshots, content.Snapshots.Count(s => s.Status == BelemetrySnapshotStatus.Captured && capture.IsSnapshotExcluded(s.Name) is false),
                          content.Snapshots.Count(s => s.Status != BelemetrySnapshotStatus.Captured || capture.IsSnapshotExcluded(s.Name)), null),
            Section(capture, BelemetryBundleSections.Description, capture.Includes(BelemetryBundleSections.Description) && capture.Description is not null ? 1 : 0, 0, null),
        ];
    }

    private static BelemetryBundleSection Section(BelemetryDiagnosticCapture capture, BelemetryBundleSections section, int items, int omitted, string? note)
    {
        if (capture.Includes(section) is false) return new BelemetryBundleSection(section, "excluded", 0, items + omitted, "excluded in the preview");
        var status = items == 0 ? "empty" : omitted > 0 ? "truncated" : "included";
        return new BelemetryBundleSection(section, status, items, omitted, note);
    }

    public static List<string> Missing(BelemetryDiagnosticCapture capture, BelemetryCaptureContent content)
    {
        var missing = new List<string>(content.Unavailable);

        if (content.ConsentOmitted > 0) missing.Add($"{content.ConsentOmitted} events of features the user has not granted were left out (consent).");
        if (content.LookbackOmitted > 0) missing.Add($"{content.LookbackOmitted} events older than the capture's lookback were left out.");
        if (content.LimitOmitted > 0) missing.Add($"{content.LimitOmitted} older events were left out at the bundle size limit ({content.Options.MaxBundleBytes.ToString(CultureInfo.InvariantCulture)} bytes).");

        foreach (var (reason, count) in content.Dropped.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var text = reason switch
            {
                "activity_limit" => "older activity events were evicted from the timeline (MaxEvents)",
                "failure_limit" => "older failure events were evicted from the timeline (MaxFailureEvents)",
                "memory_limit" => "events were evicted at the timeline's memory budget (MaxMemoryBytes)",
                "consent_revoked" => "events were purged when the user revoked consent",
                "exception_group_limit" => "exception groups were forgotten (MaxExceptionGroups)",
                "browser.capacity" => "older browser events were evicted from the browser timeline (BrowserMaxEvents)",
                "browser.malformed" => "browser events could not be read",
                _ => null,
            };
            if (text is not null) missing.Add($"{count.ToString(CultureInfo.InvariantCulture)} {text}.");
        }

        if (content.Components.All(c => c.Ancestors is null) && content.Components.Count > 0) missing.Add("The component hierarchy is unavailable: Belemetry names component types from the framework's diagnostics only (BevTools adds the tree).");
        if (capture.UserChanges.Count > 0) missing.Add($"The user narrowed the bundle in the preview ({capture.UserChanges.Count.ToString(CultureInfo.InvariantCulture)} changes, listed in the manifest).");
        return missing;
    }
}
