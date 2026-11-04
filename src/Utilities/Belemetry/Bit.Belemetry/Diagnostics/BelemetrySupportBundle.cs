using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// The support bundle format: a ZIP of JSON documents plus a Markdown report, described by a versioned manifest.
/// </summary>
/// <remarks>
/// <para>
/// Files: <c>manifest.json</c> (format, schema version, capture metadata, sections, every file's size and SHA-256,
/// limits, dropped and omitted evidence, consent, the user's preview changes), <c>report.json</c> and <c>report.md</c>
/// (the issue report), and - when included - <c>release.json</c>, <c>exceptions.json</c>, <c>timeline.json</c>,
/// <c>components.json</c> and <c>snapshots.json</c>. Every document is data: no HTML, no scripts, no serialized objects.
/// </para>
/// <para>
/// The format is versioned by <see cref="SchemaVersion"/>: a reader accepts its own version and older ones, refuses a
/// newer one, and ignores files it does not know. Writing is deterministic (sorted keys, fixed entry times).
/// </para>
/// </remarks>
public static class BelemetrySupportBundle
{
    /// <summary>The manifest's <c>format</c>.</summary>
    public const string Format = "bit-belemetry-support-bundle";

    /// <summary>The schema version written, and the newest one read.</summary>
    public const int SchemaVersion = 1;

    /// <summary>The largest bundle <see cref="ReadAsync"/> accepts, in bytes.</summary>
    public const int MaxReadBytes = 32 * 1024 * 1024;

    /// <summary>The largest file inside a bundle <see cref="ReadAsync"/> accepts, uncompressed, in bytes.</summary>
    public const int MaxEntryBytes = 16 * 1024 * 1024;

    /// <summary>The most files a bundle may hold.</summary>
    public const int MaxEntries = 32;

    private static readonly string[] _knownFiles = ["manifest.json", "report.json", "report.md", "release.json", "exceptions.json", "timeline.json", "components.json", "snapshots.json"];

    private static readonly JsonWriterOptions _writerOptions = new() { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default };

    internal static byte[] Write(BelemetryDiagnosticCapture capture, CancellationToken cancellationToken)
    {
        var content = capture.Content;
        var files = new List<(string Name, byte[] Bytes)>
        {
            ("report.md", Encoding.UTF8.GetBytes(capture.Report.ToMarkdown())),
            ("report.json", Json(json => capture.Report.WriteJson(json))),
        };

        if (capture.Release is { } release) files.Add(("release.json", Json(json => WriteRelease(json, release))));
        if (capture.Includes(BelemetryBundleSections.Exceptions)) files.Add(("exceptions.json", Json(json => WriteGroups(json, capture.ExceptionGroups))));

        cancellationToken.ThrowIfCancellationRequested();

        var events = capture.Events;
        if (events.Count > 0) files.Add(("timeline.json", Json(json => WriteTimeline(json, events, capture.RedactedKeys))));
        if (capture.Includes(BelemetryBundleSections.Components)) files.Add(("components.json", Json(json => WriteComponents(json, capture.Components))));
        var snapshots = capture.Snapshots;
        if (snapshots.Count > 0) files.Add(("snapshots.json", Json(json => WriteSnapshots(json, snapshots))));

        cancellationToken.ThrowIfCancellationRequested();

        var manifest = Json(json => WriteManifest(json, capture, content, files));

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            // A fixed entry time (the capture's) keeps the archive deterministic; ZIP times start in 1980.
            var time = capture.CreatedAt.Year < 1980 ? new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero) : capture.CreatedAt;
            foreach (var (name, bytes) in files.Prepend(("manifest.json", manifest)))
            {
                var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
                entry.LastWriteTime = time;
                using var stream = entry.Open();
                stream.Write(bytes);
            }
        }

        return buffer.ToArray();
    }

    private static byte[] Json(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>(4096);
        using (var json = new Utf8JsonWriter(buffer, _writerOptions)) write(json);
        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteManifest(Utf8JsonWriter json, BelemetryDiagnosticCapture capture, BelemetryCaptureContent content, List<(string Name, byte[] Bytes)> files)
    {
        json.WriteStartObject();
        json.WriteString("format", Format);
        json.WriteNumber("schemaVersion", SchemaVersion);
        json.WriteStartObject("generator");
        json.WriteString("name", "Bit.Belemetry");
        json.WriteString("version", BelemetrySettings.SdkVersion);
        json.WriteEndObject();
        json.WriteString("captureId", capture.Id);
        json.WriteString("createdAt", capture.CreatedAt.ToUniversalTime());
        json.WriteString("host", content.Host.ToString());
        json.WriteStartObject("trigger");
        json.WriteString("kind", capture.Trigger.Kind == BelemetryCaptureTriggerKind.Manual ? "manual" : "automatic");
        BelemetryDiagnosticJson.WriteOptional(json, "reason", capture.Trigger.Reason);
        if (capture.Subject is { } subject) json.WriteString("event", BelemetryDiagnosticJson.EventId(subject));
        BelemetryDiagnosticJson.WriteOptional(json, "fingerprint", capture.Trigger.Fingerprint);
        json.WriteEndObject();

        json.WriteString("ordering", "Events are ordered by time, then by sequence. Sequences count per source (.NET, and browser events prefixed 'b'); elapsedMs is each source's monotonic clock.");
        json.WriteString("fingerprintVersion", BelemetryExceptionFingerprint.Version);

        json.WriteStartArray("sections");
        foreach (var section in capture.Sections)
        {
            json.WriteStartObject();
            json.WriteString("section", section.Section.ToString());
            json.WriteString("status", section.Status);
            json.WriteNumber("items", section.Items);
            json.WriteNumber("omitted", section.Omitted);
            BelemetryDiagnosticJson.WriteOptional(json, "note", section.Note);
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteStartArray("files");
        foreach (var (name, bytes) in files)
        {
            json.WriteStartObject();
            json.WriteString("name", name);
            json.WriteNumber("bytes", bytes.Length);
            json.WriteString("sha256", Convert.ToHexStringLower(SHA256.HashData(bytes)));
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteStartObject("privacy");
        json.WriteString("sanitization", "Values were sanitized when recorded (key redaction, privacy patterns, size limits) and again when exported. Diagnostics keep metadata and allowlisted values only.");
        json.WriteNumber("redactedValues", capture.Events.Sum(e => e.Redactions) + capture.Snapshots.Sum(s => s.Redactions));
        json.WriteNumber("truncatedEvents", capture.Events.Count(e => e.Truncated));
        BelemetryDiagnosticJson.WriteStrings(json, "userRedactedAttributes", capture.RedactedAttributes.Order(StringComparer.Ordinal));
        BelemetryDiagnosticJson.WriteStrings(json, "userChanges", capture.UserChanges);
        json.WriteNumber("consentGranted", (int)content.Granted);
        json.WriteBoolean("consentDecided", content.ConsentDecided);
        json.WriteEndObject();

        json.WriteStartObject("limits");
        json.WriteNumber("maxEvents", content.Options.MaxEvents);
        json.WriteNumber("maxFailureEvents", content.Options.MaxFailureEvents);
        json.WriteNumber("maxBundleBytes", content.Options.MaxBundleBytes);
        json.WriteNumber("browserMaxEvents", content.Options.BrowserMaxEvents);
        json.WriteEndObject();

        json.WriteStartObject("dropped");
        foreach (var (reason, count) in content.Dropped.OrderBy(p => p.Key, StringComparer.Ordinal)) json.WriteNumber(reason, count);
        json.WriteEndObject();

        BelemetryDiagnosticJson.WriteStrings(json, "missingEvidence", capture.MissingEvidence);
        json.WriteEndObject();
    }

    private static void WriteRelease(Utf8JsonWriter json, BelemetryReleaseInfo release)
    {
        json.WriteStartObject();
        json.WriteNumber("schemaVersion", SchemaVersion);
        json.WriteString("serviceName", release.ServiceName);
        BelemetryDiagnosticJson.WriteOptional(json, "serviceVersion", release.ServiceVersion);
        BelemetryDiagnosticJson.WriteOptional(json, "serviceNamespace", release.ServiceNamespace);
        BelemetryDiagnosticJson.WriteOptional(json, "environment", release.Environment);
        BelemetryDiagnosticJson.WriteOptional(json, "commit", release.Commit);
        BelemetryDiagnosticJson.WriteOptional(json, "buildId", release.BuildId);
        BelemetryDiagnosticJson.WriteOptional(json, "configurationVersion", release.ConfigurationVersion);
        json.WriteString("host", release.Host.ToString());
        BelemetryDiagnosticJson.WriteOptional(json, "runtime", release.Runtime);
        BelemetryDiagnosticJson.WriteOptional(json, "operatingSystem", release.OperatingSystem);
        BelemetryDiagnosticJson.WriteOptional(json, "architecture", release.Architecture);
        BelemetryDiagnosticJson.WriteOptional(json, "sdkVersion", release.SdkVersion);
        WriteMap(json, "metadata", release.Metadata);
        WriteMap(json, "browser", release.Browser);
        json.WriteEndObject();

        static void WriteMap(Utf8JsonWriter json, string name, IReadOnlyDictionary<string, string> map)
        {
            json.WriteStartObject(name);
            foreach (var (key, value) in map.OrderBy(p => p.Key, StringComparer.Ordinal)) json.WriteString(key, value);
            json.WriteEndObject();
        }
    }

    private static void WriteGroups(Utf8JsonWriter json, IReadOnlyList<BelemetryExceptionGroup> groups)
    {
        json.WriteStartObject();
        json.WriteNumber("schemaVersion", SchemaVersion);
        json.WriteStartArray("groups");
        foreach (var group in groups)
        {
            json.WriteStartObject();
            json.WriteString("fingerprint", group.Fingerprint);
            json.WriteString("fingerprintVersion", group.FingerprintVersion);
            json.WriteString("type", group.ExceptionType);
            BelemetryDiagnosticJson.WriteOptional(json, "messageTemplate", group.MessageTemplate);
            BelemetryDiagnosticJson.WriteOptional(json, "anchor", group.Anchor);
            BelemetryDiagnosticJson.WriteOptional(json, "classification", group.Classification);
            json.WriteNumber("count", group.Count);
            json.WriteString("firstSeen", group.FirstSeen.ToUniversalTime());
            json.WriteString("lastSeen", group.LastSeen.ToUniversalTime());
            BelemetryDiagnosticJson.WriteStrings(json, "releases", group.Releases);
            json.WriteStartArray("occurrences");
            foreach (var occurrence in group.Occurrences)
            {
                json.WriteStartObject();
                if (occurrence.Sequence is { } sequence) json.WriteNumber("seq", sequence);
                json.WriteString("time", occurrence.Time.ToUniversalTime());
                BelemetryDiagnosticJson.WriteOptional(json, "release", occurrence.Release);
                BelemetryDiagnosticJson.WriteOptional(json, "traceId", occurrence.TraceId);
                BelemetryDiagnosticJson.WriteOptional(json, "route", occurrence.Route);
                BelemetryDiagnosticJson.WriteOptional(json, "component", occurrence.ComponentType);
                BelemetryDiagnosticJson.WriteOptional(json, "message", occurrence.Message);
                json.WriteString("origin", occurrence.Origin);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteTimeline(Utf8JsonWriter json, IReadOnlyList<BelemetryDiagnosticEvent> events, ISet<string> redactedKeys)
    {
        json.WriteStartObject();
        json.WriteNumber("schemaVersion", SchemaVersion);
        json.WriteStartArray("events");
        foreach (var e in events) BelemetryDiagnosticJson.WriteEvent(json, e, redactedKeys);
        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteComponents(Utf8JsonWriter json, IReadOnlyList<BelemetryComponentInfo> components)
    {
        json.WriteStartObject();
        json.WriteNumber("schemaVersion", SchemaVersion);
        json.WriteStartArray("components");
        foreach (var component in components)
        {
            json.WriteStartObject();
            json.WriteString("type", component.Type);
            BelemetryDiagnosticJson.WriteOptional(json, "instance", component.InstanceKey);
            json.WriteString("association", component.Association);
            json.WriteString("source", component.Source);
            json.WriteNumber("events", component.Events);
            json.WriteNumber("failures", component.Failures);
            if (component.RenderCount is { } renders) json.WriteNumber("renderCount", renders);
            if (component.RenderMilliseconds is { } ms) json.WriteNumber("renderMs", Math.Round(ms, 3));
            if (component.Ancestors is { } ancestors) BelemetryDiagnosticJson.WriteStrings(json, "ancestors", ancestors);
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteSnapshots(Utf8JsonWriter json, IReadOnlyList<BelemetryDiagnosticSnapshot> snapshots)
    {
        json.WriteStartObject();
        json.WriteNumber("schemaVersion", SchemaVersion);
        json.WriteString("note", "Snapshots describe application state; they are not a restorable state.");
        json.WriteStartArray("snapshots");
        foreach (var snapshot in snapshots)
        {
            json.WriteStartObject();
            json.WriteString("name", snapshot.Name);
            BelemetryDiagnosticJson.WriteOptional(json, "label", snapshot.Label);
            json.WriteString("status", snapshot.Status.ToString().ToLowerInvariant());
            json.WriteString("time", snapshot.Time.ToUniversalTime());
            json.WriteNumber("redactions", snapshot.Redactions);
            json.WriteNumber("truncations", snapshot.Truncations);
            json.WriteNumber("omissions", snapshot.Omissions);
            BelemetryDiagnosticJson.WriteOptional(json, "error", snapshot.Error);
            if (snapshot.Data is { } data)
            {
                json.WritePropertyName("data");
                BelemetryDiagnosticJson.WriteValue(json, data, depth: 0);
            }
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteEndObject();
    }

    /// <summary>
    /// Reads and validates a support bundle: the manifest's format and schema version, every listed file's size and
    /// SHA-256, and the report and timeline. Decompression is bounded (<see cref="MaxReadBytes"/>,
    /// <see cref="MaxEntryBytes"/>, <see cref="MaxEntries"/>), nothing is extracted to disk, and files it does not know
    /// are ignored. It never throws for a malformed bundle: <see cref="BelemetrySupportBundleContents.IsValid"/> and
    /// <see cref="BelemetrySupportBundleContents.Errors"/> say what is wrong.
    /// </summary>
    public static async Task<BelemetrySupportBundleContents> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var errors = new List<string>();
        var warnings = new List<string>();
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        byte[] archive;
        try
        {
            archive = await ReadBoundedAsync(stream, MaxReadBytes, cancellationToken);
        }
        catch (InvalidDataException ex)
        {
            return BelemetrySupportBundleContents.Invalid(ex.Message);
        }

        try
        {
            using var zip = new ZipArchive(new MemoryStream(archive, writable: false), ZipArchiveMode.Read);
            if (zip.Entries.Count > MaxEntries) return BelemetrySupportBundleContents.Invalid($"The bundle holds {zip.Entries.Count} files; at most {MaxEntries} are accepted.");

            long total = 0;
            foreach (var entry in zip.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.FullName.Contains('/') || entry.FullName.Contains('\\') || entry.FullName.Contains("..", StringComparison.Ordinal))
                {
                    errors.Add($"The file name '{Clean(entry.FullName)}' is not allowed (folders and relative paths are not part of the format).");
                    continue;
                }
                if (Array.IndexOf(_knownFiles, entry.FullName) < 0)
                {
                    warnings.Add($"The file '{Clean(entry.FullName)}' is not part of the format and was ignored.");
                    continue;
                }
                if (entry.Length > MaxEntryBytes) return BelemetrySupportBundleContents.Invalid($"The file '{entry.FullName}' is larger than {MaxEntryBytes} bytes.");

                // The declared size can lie: the read itself is bounded too (a decompression bomb stops at the limit).
                await using var entryStream = entry.Open();
                var bytes = await ReadBoundedAsync(entryStream, MaxEntryBytes, cancellationToken);
                total += bytes.Length;
                if (total > MaxReadBytes * 2L) return BelemetrySupportBundleContents.Invalid("The bundle's files are too large in total.");
                files[entry.FullName] = bytes;
            }
        }
        catch (InvalidDataException ex)
        {
            return BelemetrySupportBundleContents.Invalid("The bundle is not a valid ZIP archive: " + ex.Message);
        }

        if (files.TryGetValue("manifest.json", out var manifestBytes) is false) return BelemetrySupportBundleContents.Invalid("The bundle has no manifest.json.");

        using var manifest = Parse(manifestBytes, "manifest.json", errors);
        if (manifest is null) return BelemetrySupportBundleContents.Invalid(errors);

        var root = manifest.RootElement;
        if (BelemetryDiagnosticJson.String(root, "format") != Format) return BelemetrySupportBundleContents.Invalid($"The manifest's format is not '{Format}'.");
        var version = BelemetryDiagnosticJson.Int(root, "schemaVersion");
        if (version is null or < 1) return BelemetrySupportBundleContents.Invalid("The manifest has no valid schemaVersion.");
        if (version > SchemaVersion) return BelemetrySupportBundleContents.Invalid($"The bundle's schema version {version} is newer than this reader's ({SchemaVersion}); update Bit.Belemetry to read it.");

        if (root.TryGetProperty("files", out var listed) && listed.ValueKind == JsonValueKind.Array)
        {
            foreach (var file in listed.EnumerateArray().Take(MaxEntries))
            {
                var name = BelemetryDiagnosticJson.String(file, "name");
                if (name is null) continue;
                if (files.TryGetValue(name, out var bytes) is false)
                {
                    errors.Add($"The manifest lists '{Clean(name)}', which the bundle does not hold.");
                    continue;
                }
                if (BelemetryDiagnosticJson.Long(file, "bytes") is { } size && size != bytes.Length) errors.Add($"'{name}' does not have the size the manifest says.");
                if (BelemetryDiagnosticJson.String(file, "sha256") is { } hash && string.Equals(hash, Convert.ToHexStringLower(SHA256.HashData(bytes)), StringComparison.OrdinalIgnoreCase) is false)
                {
                    errors.Add($"'{name}' does not match its SHA-256 in the manifest: it was changed or damaged.");
                }
            }
        }
        else
        {
            errors.Add("The manifest lists no files.");
        }

        BelemetryIssueReport? report = null;
        if (files.TryGetValue("report.json", out var reportBytes))
        {
            using var reportJson = Parse(reportBytes, "report.json", errors);
            if (reportJson is not null)
            {
                report = BelemetryIssueReport.FromJson(reportJson.RootElement);
                if (report is null) errors.Add("report.json is not a supported issue report.");
            }
        }
        else
        {
            errors.Add("The bundle has no report.json.");
        }

        var events = new List<BelemetryDiagnosticEvent>();
        if (files.TryGetValue("timeline.json", out var timelineBytes))
        {
            using var timeline = Parse(timelineBytes, "timeline.json", errors);
            if (timeline is not null && timeline.RootElement.TryGetProperty("events", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray().Take(BelemetryDiagnosticJson.MaxArrayItems))
                {
                    if (BelemetryDiagnosticJson.ReadEvent(item) is { } e) events.Add(e);
                }
            }
        }

        var groups = new List<BelemetryExceptionGroup>();
        if (files.TryGetValue("exceptions.json", out var groupBytes))
        {
            using var groupJson = Parse(groupBytes, "exceptions.json", errors);
            if (groupJson is not null) groups = ReadGroups(groupJson.RootElement);
        }

        return new BelemetrySupportBundleContents
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            SchemaVersion = version.Value,
            CaptureId = BelemetryDiagnosticJson.String(root, "captureId"),
            CreatedAt = BelemetryDiagnosticJson.Time(root, "createdAt"),
            Generator = root.TryGetProperty("generator", out var generator) ? $"{BelemetryDiagnosticJson.String(generator, "name")} {BelemetryDiagnosticJson.String(generator, "version")}".Trim() : null,
            Report = report,
            ReportMarkdown = files.TryGetValue("report.md", out var md) ? Encoding.UTF8.GetString(md) : null,
            Events = events,
            ExceptionGroups = groups,
            MissingEvidence = BelemetryDiagnosticJson.Strings(root, "missingEvidence"),
            Files = files.ToDictionary(p => p.Key, p => (long)p.Value.Length, StringComparer.Ordinal),
        };
    }

    private static List<BelemetryExceptionGroup> ReadGroups(JsonElement root)
    {
        var groups = new List<BelemetryExceptionGroup>();
        if (root.TryGetProperty("groups", out var items) is false || items.ValueKind != JsonValueKind.Array) return groups;

        foreach (var g in items.EnumerateArray().Take(1000))
        {
            if (g.ValueKind != JsonValueKind.Object) continue;
            var occurrences = new List<BelemetryExceptionOccurrence>();
            if (g.TryGetProperty("occurrences", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var o in list.EnumerateArray().Take(50))
                {
                    occurrences.Add(new BelemetryExceptionOccurrence(BelemetryDiagnosticJson.Long(o, "seq"), BelemetryDiagnosticJson.Time(o, "time") ?? default, BelemetryDiagnosticJson.String(o, "release"),
                        BelemetryDiagnosticJson.String(o, "traceId"), BelemetryDiagnosticJson.String(o, "route"), BelemetryDiagnosticJson.String(o, "component"),
                        BelemetryDiagnosticJson.String(o, "message"), BelemetryDiagnosticJson.String(o, "origin") ?? "log"));
                }
            }

            groups.Add(new BelemetryExceptionGroup
            {
                Fingerprint = BelemetryDiagnosticJson.String(g, "fingerprint") ?? string.Empty,
                FingerprintVersion = BelemetryDiagnosticJson.String(g, "fingerprintVersion") ?? BelemetryExceptionFingerprint.Version,
                ExceptionType = BelemetryDiagnosticJson.String(g, "type") ?? "Exception",
                MessageTemplate = BelemetryDiagnosticJson.String(g, "messageTemplate"),
                Anchor = BelemetryDiagnosticJson.String(g, "anchor"),
                Classification = BelemetryDiagnosticJson.String(g, "classification"),
                Count = BelemetryDiagnosticJson.Int(g, "count") ?? occurrences.Count,
                FirstSeen = BelemetryDiagnosticJson.Time(g, "firstSeen") ?? default,
                LastSeen = BelemetryDiagnosticJson.Time(g, "lastSeen") ?? default,
                Releases = BelemetryDiagnosticJson.Strings(g, "releases"),
                Occurrences = occurrences,
            });
        }
        return groups;
    }

    private static JsonDocument? Parse(byte[] bytes, string name, List<string> errors)
    {
        try
        {
            return JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 64 });
        }
        catch (JsonException ex)
        {
            errors.Add($"{name} is not valid JSON: {ex.Message}");
            return null;
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(Stream stream, int max, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken)) > 0)
            {
                if (buffer.Length + read > max) throw new InvalidDataException($"The bundle is larger than {max.ToString(CultureInfo.InvariantCulture)} bytes.");
                buffer.Write(chunk, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }
        return buffer.ToArray();
    }

    private static string Clean(string name) => new(name.Take(100).Select(c => char.IsControl(c) ? '?' : c).ToArray());
}
