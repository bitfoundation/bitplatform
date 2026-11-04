using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Builds a capture: the timeline (consent-filtered, within the lookback), the browser's timeline, the server side of the
/// page's requests, the exception groups, the components, the snapshots and the release - each source isolated, so one
/// that fails or is unavailable is reported as missing instead of failing the capture.
/// </summary>
internal sealed class BelemetryDiagnosticCaptureBuilder(BelemetryDiagnosticHub hub, BelemetryDiagnosticRecorder recorder, BelemetryDiagnosticsService? session)
{
    private static readonly TimeSpan _providerTimeout = TimeSpan.FromSeconds(2);

    public async Task<BelemetryDiagnosticCapture> BuildAsync(BelemetryDiagnosticCaptureOptions options, BelemetryCaptureTrigger trigger, CancellationToken cancellationToken)
    {
        using var _ = BelemetryDiagnosticHub.Suppress();

        var settings = hub.Settings;
        var content = new BelemetryCaptureContent
        {
            Id = BelemetryCaptureContent.NewId(),
            CreatedAt = DateTimeOffset.UtcNow,
            Trigger = trigger,
            Options = hub.Options,
            Sanitizer = hub.Sanitizer,
            Host = settings.Host,
            BrowserRequested = options.IncludeBrowserEvidence,
            SnapshotsRequested = options.IncludeSnapshots,
            MayCapture = hub.MayCapture,
            MayCaptureBrowser = BrowserConsent(),
            CurrentGrant = () => hub.ConsentState().Granted | (BelemetryFeatures)(session?.Bridge.Context?.Consent ?? 0),
        };
        (content.Granted, content.ConsentDecided) = hub.ConsentState();

        if (options.Description is { } description)
        {
            var counters = new BelemetrySanitizeCounters();
            content.Description = string.IsNullOrWhiteSpace(description) ? null : hub.Sanitizer.Text(description, 4000, ref counters);
        }

        // The .NET timeline, as the user's consent allows it now (a record held while undecided is not captured).
        var lookbackStart = options.Lookback is { } lookback && lookback > TimeSpan.Zero ? BelemetryTime.NowUnixNano() - (lookback.Ticks * 100) : long.MinValue;
        var events = new List<BelemetryDiagnosticEvent>();
        foreach (var e in recorder.Events())
        {
            if (hub.MayCapture(e.Feature) is false)
            {
                content.ConsentOmitted++;
                continue;
            }
            if (e.IsFailure is false && e.TimeUnixNano < lookbackStart)
            {
                content.LookbackOmitted++;
                continue;
            }
            if (options.IncludeRecentActivity is false && e.IsFailure is false && e.Severity < BelemetrySeverity.Warn)
            {
                content.LookbackOmitted++;
                continue;
            }
            events.Add(e);
        }
        foreach (var (reason, count) in recorder.Dropped()) content.AddDropped(reason, count);

        // The exception the app has in hand becomes the subject.
        BelemetryDiagnosticEvent? subject = null;
        if (options.Exception is { } exception)
        {
            var counters = new BelemetrySanitizeCounters();
            var converted = hub.Factory.FromException(exception, "tracked", ref counters);

            // Observed through the timeline already (an ErrorBoundary or the renderer logged it moments ago): that event is
            // the subject, with its origin and trace, rather than a second report of one failure.
            var recent = BelemetryTime.NowUnixNano() - (TimeSpan.FromMinutes(1).Ticks * 100);
            subject = converted is null ? null : events.LastOrDefault(e => e.Exception?.Fingerprint == converted.Fingerprint && e.TimeUnixNano >= recent);

            if (subject is null && converted is not null)
            {
                var activity = Activity.Current;
                var component = BelemetryDiagnosticEventFactory.InferComponent(converted);
                subject = new BelemetryDiagnosticEvent
                {
                    Sequence = recorder.NextSequence(),
                    TimeUnixNano = BelemetryTime.NowUnixNano(),
                    ElapsedMilliseconds = recorder.ElapsedMilliseconds(),
                    Category = BelemetryDiagnosticCategories.Exception,
                    Name = "exception",
                    Severity = BelemetrySeverity.Error,
                    Outcome = BelemetryDiagnosticOutcome.Failure,
                    Source = BelemetryDiagnosticSource.Application,
                    Feature = BelemetryFeatures.Errors,
                    TraceId = activity?.IdFormat == ActivityIdFormat.W3C ? activity.TraceId.ToHexString() : null,
                    SpanId = activity?.IdFormat == ActivityIdFormat.W3C ? activity.SpanId.ToHexString() : null,
                    ComponentType = component,
                    ComponentSource = component is null ? null : "stack",
                    Exception = converted,
                    Redactions = counters.Redactions,
                    Truncated = counters.Truncations > 0,
                };
                events.Add(subject);
            }
        }

        // The browser's timeline.
        var browserInfo = new Dictionary<string, string>(StringComparer.Ordinal);
        if (options.IncludeBrowserEvidence)
        {
            var bridge = session?.Bridge;
            JsonElement? payload = bridge is null ? null : await bridge.GetDiagnosticsAsync(cancellationToken);
            if (payload is { } json)
            {
                var browser = BelemetryBrowserEvidence.Read(json, hub);
                content.BrowserAvailable = true;

                // The capture's lookback and activity choice apply to the page's events as to .NET's.
                foreach (var e in browser.Events)
                {
                    if (e.IsFailure is false && (e.TimeUnixNano < lookbackStart || (options.IncludeRecentActivity is false && e.Severity < BelemetrySeverity.Warn)))
                    {
                        content.LookbackOmitted++;
                        continue;
                    }
                    events.Add(e);
                }
                foreach (var (key, value) in browser.Browser) browserInfo[key] = value;
                foreach (var (reason, count) in browser.Dropped) content.AddDropped(reason, count);
            }
            else
            {
                content.Unavailable.Add(bridge is null
                    ? "Browser evidence: no browser is connected to this scope (a background capture without a session)."
                    : "Browser evidence: the browser script was not reachable (prerendering, a disconnected circuit, the script not loaded, or diagnostics off in the page).");
            }
        }

        // A server's side of the page's own requests: the trace ids the page generated are the association.
        if (hub.TraceIndex is { } index)
        {
            var pageTraces = events.Where(e => e.Source == BelemetryDiagnosticSource.Browser && e.Category == BelemetryDiagnosticCategories.Http && e.TraceId is not null).Select(e => e.TraceId!).ToList();
            // Numbered anew: the index numbers its events apart from this circuit's timeline, and an id is unique in a bundle.
            if (pageTraces.Count > 0) events.AddRange(index.Find(pageTraces).Where(e => hub.MayCapture(e.Feature)).Select(e => e.WithSequence(recorder.NextSequence())));
        }
        else if (settings.IsServer is false && settings.Host == BelemetryHostKind.WebAssembly)
        {
            content.Unavailable.Add("Server-side evidence: a WebAssembly app's server is another process; look its traces up by the trace ids in the report.");
        }

        if (settings.IsServer && settings.CollectDotnet is false)
        {
            content.Unavailable.Add("Server-side spans: the server pipeline is off (Server.CollectDotnetTelemetry), so only the circuit's logged errors and framework activities are on the timeline.");
        }

        events.Sort(static (a, b) => a.TimeUnixNano != b.TimeUnixNano ? a.TimeUnixNano.CompareTo(b.TimeUnixNano) : a.Sequence.CompareTo(b.Sequence));
        events = EnforceLimit(events, content);
        content.Events = events;

        subject ??= trigger.EventSequence is { } sequence ? events.LastOrDefault(e => e.Sequence == sequence && e.Source != BelemetryDiagnosticSource.Browser && e.Exception is not null) : null;
        subject ??= trigger.Fingerprint is { } fingerprint ? events.LastOrDefault(e => e.Exception?.Fingerprint == fingerprint) : null;
        subject ??= events.LastOrDefault(e => e.Exception is not null && e.Exception.Origin is "unhandled" or "error-boundary" or "circuit");
        subject ??= events.LastOrDefault(e => e.Exception is not null);
        content.Subject = subject;

        content.Groups = Groups(events, content);
        content.Components = await ComponentsAsync(options, events, subject, content, cancellationToken);

        if (options.IncludeSnapshots)
        {
            var context = new BelemetrySnapshotContext { Reason = options.Reason, ForCapture = true, Exception = subject?.Exception, Route = subject?.Route ?? events.LastOrDefault(e => e.Route is not null)?.Route };
            var providers = session?.SnapshotProviders() ?? [];
            content.Snapshots = [.. recorder.Snapshots(), .. await TakeSnapshotsAsync(hub, providers, options.SnapshotProviders, context, label: null, cancellationToken)];
        }

        content.Release = Release(settings, hub, browserInfo);

        if (content.Components.Count == 0 && events.Count > 0) content.Unavailable.Add("Components: no event of the timeline names a component (the framework's component activities need .NET 10; they are not reported for static rendering).");
        return new BelemetryDiagnosticCapture(content);
    }

    /// <summary>
    /// The page's consent for its own events: on a client host the state .NET keeps in step with the script; on a server
    /// the grant the page last reported over this circuit (a revoke made in the page reaches it as a context change).
    /// </summary>
    private Func<BelemetryFeatures, bool> BrowserConsent()
    {
        if (hub.Settings.IsServer is false) return hub.MayCapture;
        var bridge = session?.Bridge;
        return feature => bridge?.Context is not { } context || ((BelemetryFeatures)context.Consent & feature) == feature;
    }

    /// <summary>Keeps the timeline under <see cref="BelemetryDiagnosticsOptions.MaxBundleBytes"/>: the oldest activity goes first, then the oldest failures.</summary>
    private static List<BelemetryDiagnosticEvent> EnforceLimit(List<BelemetryDiagnosticEvent> events, BelemetryCaptureContent content)
    {
        // JSON is roughly the estimate's half (UTF-8, not UTF-16) plus its field names; the estimate errs on the large side.
        var budget = Math.Max(64 * 1024, content.Options.MaxBundleBytes) * 3L / 4;
        long total = events.Sum(e => (long)e.EstimatedBytes);
        if (total <= budget) return events;

        var removed = new HashSet<BelemetryDiagnosticEvent>(ReferenceEqualityComparer.Instance);
        foreach (var pass in new[] { false, true })
        {
            foreach (var e in events)
            {
                if (total <= budget) break;
                if (e.IsFailure != pass) continue;
                removed.Add(e);
                total -= e.EstimatedBytes;
            }
        }

        content.LimitOmitted = removed.Count;
        return events.Where(e => removed.Contains(e) is false).ToList();
    }

    /// <summary>The exception groups of the captured exceptions: the timeline's (with their full history) and the browser's.</summary>
    private List<BelemetryExceptionGroup> Groups(List<BelemetryDiagnosticEvent> events, BelemetryCaptureContent content)
    {
        var groups = hub.MayCapture(BelemetryFeatures.Errors) ? recorder.Groups() : [];
        var known = new HashSet<string>(groups.Select(g => g.Fingerprint), StringComparer.Ordinal);

        foreach (var byFingerprint in events.Where(e => e.Exception is not null && known.Contains(e.Exception.Fingerprint) is false).GroupBy(e => e.Exception!.Fingerprint, StringComparer.Ordinal))
        {
            var list = byFingerprint.ToList();
            var first = list[0].Exception!;
            if (list.All(e => e.Source == BelemetryDiagnosticSource.Browser)) content.BrowserGroups.Add(byFingerprint.Key);
            groups.Add(new BelemetryExceptionGroup
            {
                Fingerprint = byFingerprint.Key,
                FingerprintVersion = first.CustomFingerprint ? "custom" : BelemetryExceptionFingerprint.Version,
                ExceptionType = first.Type,
                MessageTemplate = first.MessageTemplate,
                Anchor = first.Anchor,
                Classification = first.Classification,
                Count = list.Count,
                FirstSeen = BelemetryTime.ToDateTime(list[0].TimeUnixNano),
                LastSeen = BelemetryTime.ToDateTime(list[^1].TimeUnixNano),
                Releases = hub.Factory.Release is { } release ? [release] : [],
                Occurrences = list.TakeLast(5).Select(e => new BelemetryExceptionOccurrence(e.Sequence, BelemetryTime.ToDateTime(e.TimeUnixNano), hub.Factory.Release, e.TraceId, e.Route, e.ComponentType, e.Exception!.Message, e.Exception.Origin)).ToList(),
            });
        }

        return groups;
    }

    private async Task<List<BelemetryComponentInfo>> ComponentsAsync(BelemetryDiagnosticCaptureOptions options, List<BelemetryDiagnosticEvent> events, BelemetryDiagnosticEvent? subject, BelemetryCaptureContent content, CancellationToken cancellationToken)
    {
        if (options.IncludeComponentContext is false) return [];

        var stats = new Dictionary<string, (int Events, int Failures, int Rank, string Association)>(StringComparer.Ordinal);
        void Note(string type, bool failure, int rank, string association)
        {
            (int Events, int Failures, int Rank, string Association) current = stats.TryGetValue(type, out var s) ? s : (0, 0, int.MaxValue, association);
            stats[type] = (current.Events + 1, current.Failures + (failure ? 1 : 0), Math.Min(current.Rank, rank), rank < current.Rank ? association : current.Association);
        }

        // Rank: the handler whose trace the subject happened in, then a component the subject's stack names, then the rest.
        foreach (var e in events)
        {
            if (e.ComponentType is not { } type) continue;
            var inSubjectTrace = subject?.TraceId is not null && e.TraceId == subject.TraceId && e.ComponentSource == "framework";
            Note(type, e.IsFailure, inSubjectTrace ? 0 : e == subject ? 1 : 2, inSubjectTrace ? "trace" : e.ComponentSource ?? "framework");
        }

        var renders = recorder.RenderStats();
        var ordered = stats.OrderBy(p => p.Value.Rank).ThenByDescending(p => p.Value.Failures).ThenByDescending(p => p.Value.Events).Take(20).ToList();
        var components = ordered.Select((p, i) => new BelemetryComponentInfo
        {
            Type = p.Key,
            InstanceKey = "c" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Events = p.Value.Events,
            Failures = p.Value.Failures,
            Association = p.Value.Association,
            RenderCount = renders.TryGetValue(p.Key, out var r) ? r.Count : null,
            RenderMilliseconds = renders.TryGetValue(p.Key, out var rm) ? rm.Milliseconds : null,
        }).ToList();

        // A tool that knows the live tree (BevTools) adds ancestry and render facts. Its answer is sanitized like any other.
        foreach (var provider in session?.ComponentProviders() ?? [])
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(_providerTimeout);
                var described = await provider.DescribeAsync(new BelemetryComponentContextRequest
                {
                    ComponentTypes = components.Select(c => c.Type).ToList(),
                    Route = subject?.Route ?? events.LastOrDefault(e => e.Route is not null)?.Route,
                }, timeout.Token);

                content.ComponentProvider ??= provider.Name;
                components = Merge(components, described, provider.Name);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
            {
                content.Unavailable.Add($"Components: the provider '{provider.Name}' timed out.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                content.Unavailable.Add($"Components: the provider '{provider.Name}' failed ({ex.GetType().Name}).");
            }
        }

        return components;
    }

    private List<BelemetryComponentInfo> Merge(List<BelemetryComponentInfo> components, IReadOnlyList<BelemetryComponentInfo>? described, string provider)
    {
        if (described is null || described.Count == 0) return components;

        var sanitizer = hub.Sanitizer;
        var merged = components.ToList();
        foreach (var info in described.Take(50))
        {
            var counters = new BelemetrySanitizeCounters();
            if (sanitizer.Text(info.Type, 512, ref counters) is not { } type) continue;
            var ancestors = info.Ancestors?.Take(32).Select(a => sanitizer.Text(a, 512, ref counters) ?? "?").ToList();
            var index = merged.FindIndex(c => c.Type == type);
            if (index >= 0)
            {
                var existing = merged[index];
                merged[index] = new BelemetryComponentInfo
                {
                    Type = existing.Type,
                    InstanceKey = existing.InstanceKey,
                    Events = existing.Events,
                    Failures = existing.Failures,
                    Association = existing.Association,
                    Ancestors = ancestors ?? existing.Ancestors,
                    RenderCount = info.RenderCount ?? existing.RenderCount,
                    RenderMilliseconds = info.RenderMilliseconds ?? existing.RenderMilliseconds,
                    Source = provider,
                };
            }
            else if (merged.Count < 30)
            {
                merged.Add(new BelemetryComponentInfo
                {
                    Type = type,
                    InstanceKey = "c" + (merged.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Ancestors = ancestors,
                    RenderCount = info.RenderCount,
                    RenderMilliseconds = info.RenderMilliseconds,
                    Association = "provider",
                    Source = provider,
                });
            }
        }
        return merged;
    }

    /// <summary>Takes snapshots, each provider isolated: one that throws or takes too long is reported, and the rest still run.</summary>
    internal static async Task<List<BelemetryDiagnosticSnapshot>> TakeSnapshotsAsync(BelemetryDiagnosticHub hub, IEnumerable<IBelemetrySnapshotProvider> providers, IReadOnlyCollection<string>? names,
        BelemetrySnapshotContext context, string? label, CancellationToken cancellationToken)
    {
        var limits = hub.Options.Snapshots;
        var timeout = limits.Timeout > TimeSpan.Zero ? limits.Timeout : TimeSpan.FromSeconds(2);
        var snapshots = new List<BelemetryDiagnosticSnapshot>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var provider in providers)
        {
            string name;
            try
            {
                name = provider.Name;
            }
            catch
            {
                continue;
            }

            var counters = new BelemetrySanitizeCounters();
            name = hub.Sanitizer.Text(name, 64, ref counters) ?? "snapshot";
            if (seen.Add(name) is false) continue;
            if (names is not null && names.Contains(name) is false) continue;

            var time = DateTimeOffset.UtcNow;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                var task = provider.CaptureAsync(context, cts.Token).AsTask();
                var finished = await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));
                if (finished != task)
                {
                    // Observed, so a late failure is not an unobserved task exception.
                    _ = task.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    cancellationToken.ThrowIfCancellationRequested();
                    snapshots.Add(new BelemetryDiagnosticSnapshot { Name = name, Label = label, Status = BelemetrySnapshotStatus.TimedOut, Time = time });
                    continue;
                }

                var data = await task;
                if (data is null || data.Count == 0)
                {
                    snapshots.Add(new BelemetryDiagnosticSnapshot { Name = name, Label = label, Status = BelemetrySnapshotStatus.Empty, Time = time });
                    continue;
                }

                var tree = hub.Sanitizer.Tree(data, limits, ref counters);
                snapshots.Add(new BelemetryDiagnosticSnapshot
                {
                    Name = name,
                    Label = label,
                    Status = BelemetrySnapshotStatus.Captured,
                    Time = time,
                    Data = tree,
                    Redactions = counters.Redactions,
                    Truncations = counters.Truncations,
                    Omissions = counters.Omissions,
                    Bytes = MeasureJson(tree),
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Only the type: the message of a state provider's failure may hold the state.
                snapshots.Add(new BelemetryDiagnosticSnapshot { Name = name, Label = label, Status = BelemetrySnapshotStatus.Failed, Time = time, Error = ex.GetType().FullName });
            }
        }

        return snapshots;
    }

    private static int MeasureJson(Dictionary<string, object?> tree)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>(1024);
        using (var json = new Utf8JsonWriter(buffer)) BelemetryDiagnosticJson.WriteValue(json, tree, depth: 0);
        return buffer.WrittenCount;
    }

    private static BelemetryReleaseInfo Release(BelemetrySettings settings, BelemetryDiagnosticHub hub, Dictionary<string, string> browser)
    {
        var options = settings.Options;
        var release = options.Diagnostics.Release;
        var counters = new BelemetrySanitizeCounters();
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in release.Metadata.Take(32))
        {
            if (hub.Sanitizer.Scalar(key, value, ref counters, 256) is string safe) metadata[key.Length > 64 ? key[..64] : key] = safe;
        }

        return new BelemetryReleaseInfo
        {
            ServiceName = settings.ServiceName,
            ServiceVersion = settings.ServiceVersion,
            ServiceNamespace = options.ServiceNamespace,
            Environment = options.Environment,
            Commit = hub.Sanitizer.Text(release.Commit, 128, ref counters),
            BuildId = hub.Sanitizer.Text(release.BuildId, 128, ref counters),
            ConfigurationVersion = hub.Sanitizer.Text(release.ConfigurationVersion, 128, ref counters),
            Metadata = metadata,
            Host = settings.Host,
            Runtime = RuntimeInformation.FrameworkDescription,
            OperatingSystem = OperatingSystem.IsBrowser() ? "Browser"
                            : OperatingSystem.IsWindows() ? "Windows"
                            : OperatingSystem.IsAndroid() ? "Android"
                            : OperatingSystem.IsIOS() ? "iOS"
                            : OperatingSystem.IsMacCatalyst() ? "MacCatalyst"
                            : OperatingSystem.IsMacOS() ? "macOS"
                            : OperatingSystem.IsLinux() ? "Linux"
                            : "Other",
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            SdkVersion = BelemetrySettings.SdkVersion,
            Browser = browser,
        };
    }
}
