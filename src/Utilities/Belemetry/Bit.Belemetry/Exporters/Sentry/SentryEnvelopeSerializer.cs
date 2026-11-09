using System.Buffers;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Turns the error records of a batch into Sentry events, one envelope each (an envelope holds one event). The mapping
/// is documented in CLAUDE.md ("Sentry mapping") and mirrored by <c>Scripts/exporters/sentry.ts</c>, which writes the
/// same event for a browser record.
/// </summary>
internal static class SentryEnvelopeSerializer
{
    /// <summary>The scope of the browser script's own errors (<c>error</c>, <c>unhandledrejection</c>): nothing caught them.</summary>
    internal const string BrowserErrorsScope = "bit.belemetry.errors";

    /// <param name="Dsn">The project.</param>
    /// <param name="Release">The configured release; null takes <c>{service.name}@{service.version}</c> of the resource.</param>
    /// <param name="MinimumSeverity">The least severe record that becomes an event; null for all.</param>
    /// <param name="IncludeUnhandled">Whether the records of <see cref="BelemetryUnhandledExceptions"/> become events.</param>
    /// <param name="MaskIp">Whether Sentry is told not to infer the IP address.</param>
    public sealed record Context(SentryDsn Dsn, string? Release, BelemetrySeverity? MinimumSeverity, bool IncludeUnhandled, bool MaskIp)
    {
        public bool Passes(BelemetrySeverity severity) => MinimumSeverity is null || severity >= MinimumSeverity.Value;

        /// <summary>Whether the exporter sends the log record of <paramref name="logged"/> as an event: not one dropped before any exporter had it.</summary>
        public bool Sends(BelemetryLoggedException logged) => logged.IsDropped is false && Passes(logged.Severity);
    }

    /// <summary>
    /// Whether a log record is an event: one carrying an exception, or a plain log record (no event name) with a body,
    /// at or above the minimum level. Other events (page views, custom events, web vitals) are not.
    /// </summary>
    public static bool IsEvent(BelemetryLogRecord log, Context context)
    {
        if (context.Passes(log.Severity) is false) return false;
        if (log.HasException) return context.IncludeUnhandled || log.Scope.Name != BelemetryUnhandledExceptions.ScopeName;
        return log.EventName is null && string.IsNullOrEmpty(log.Body) is false;
    }

    /// <summary>
    /// Whether an exception recorded on a span is an event of its own: not when the error limits dropped it, nor when a
    /// log record this exporter sends is the same exception (<c>LogError(ex)</c> and <c>AddException(ex)</c> are one).
    /// </summary>
    public static bool IsEvent(BelemetrySpanEvent spanEvent, Context context) =>
        spanEvent.IsException && spanEvent.Attributes.HasException && spanEvent.Limited is false
        && (spanEvent.LoggedAs is not { } logged || context.Sends(logged) is false)
        && context.Passes(BelemetrySeverity.Error);

    /// <summary>The number of events <see cref="Serialize"/> writes for <paramref name="batch"/>.</summary>
    public static int Count(BelemetryBatch batch, Context context)
    {
        var count = 0;
        foreach (var span in batch.Spans)
        {
            foreach (var spanEvent in span.Events)
            {
                if (IsEvent(spanEvent, context)) count++;
            }
        }
        foreach (var log in batch.Logs)
        {
            if (IsEvent(log, context)) count++;
        }
        return count;
    }

    /// <summary>The envelope of every event of <paramref name="batch"/>.</summary>
    public static IEnumerable<byte[]> Serialize(BelemetryBatch batch, Context context)
    {
        var resource = batch.Resource.Attributes;
        foreach (var span in batch.Spans)
        {
            foreach (var spanEvent in span.Events)
            {
                if (IsEvent(spanEvent, context) is false) continue;
                yield return Envelope(context, resource, new Source(spanEvent.Attributes, span.Attributes, BelemetrySeverity.Error, Body: null,
                                                                   span.Scope.Name, spanEvent.TimeUnixNano, span.TraceId, span.SpanId, span.ParentSpanId, span.Name, Exception: null));
            }
        }
        foreach (var log in batch.Logs)
        {
            if (IsEvent(log, context) is false) continue;
            yield return Envelope(context, resource, new Source(log.Attributes, log.Attributes, log.Severity, log.Body, log.Scope.Name,
                                                               log.TimeUnixNano, log.TraceId, log.SpanId, ParentSpanId: null, Transaction: null, log.Exception));
        }
    }

    /// <summary>What an event is made of.</summary>
    /// <param name="Attributes">The record's attributes: the exception's, and the rest as <c>extra</c>.</param>
    /// <param name="Ids">Where the session and user ids are: the record's attributes, or the span's for its exception.</param>
    /// <param name="Severity">The event's level; an exception recorded on a span is an error.</param>
    /// <param name="Body">The log message, if any.</param>
    /// <param name="Scope">The instrumentation scope: the event's logger, and whether the exception was handled.</param>
    /// <param name="TimeUnixNano">When it happened.</param>
    /// <param name="TraceId">The trace it happened in.</param>
    /// <param name="SpanId">The span it happened in.</param>
    /// <param name="ParentSpanId">That span's parent, for an exception recorded on a span.</param>
    /// <param name="Transaction">The span's name, for an exception recorded on a span.</param>
    /// <param name="Exception">The .NET exception object of a log record, for its frames' method tokens and IL offsets.</param>
    private sealed record Source(BelemetryAttributes Attributes, BelemetryAttributes Ids, BelemetrySeverity Severity, string? Body, string Scope,
                                 long TimeUnixNano, string? TraceId, string? SpanId, string? ParentSpanId, string? Transaction, Exception? Exception);

    private static byte[] Envelope(Context context, BelemetryAttributes resource, Source source)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var browser = resource.GetString(BelemetrySemanticConventions.TelemetrySdkLanguage) == "webjs";
        var sdkName = browser ? "bit.belemetry.javascript" : "bit.belemetry.dotnet";
        var sdkVersion = resource.GetString(BelemetrySemanticConventions.TelemetrySdkVersion) ?? BelemetrySettings.SdkVersion;

        var item = new ArrayBufferWriter<byte>(2048);
        using (var w = new Utf8JsonWriter(item))
        {
            WriteEvent(w, context, resource, source, eventId, browser, sdkName, sdkVersion);
        }

        var envelope = new ArrayBufferWriter<byte>(item.WrittenCount + 256);
        using (var w = new Utf8JsonWriter(envelope))
        {
            w.WriteStartObject();
            w.WriteString("event_id", eventId);
            w.WriteString("dsn", context.Dsn.PublicDsn);
            w.WriteStartObject("sdk");
            w.WriteString("name", sdkName);
            w.WriteString("version", sdkVersion);
            w.WriteEndObject();
            w.WriteEndObject();
        }
        envelope.Write("\n"u8);

        using (var w = new Utf8JsonWriter(envelope))
        {
            w.WriteStartObject();
            w.WriteString("type", "event");
            w.WriteNumber("length", item.WrittenCount);
            w.WriteEndObject();
        }
        envelope.Write("\n"u8);
        envelope.Write(item.WrittenSpan);
        envelope.Write("\n"u8);

        return envelope.WrittenSpan.ToArray();
    }

    private static void WriteEvent(Utf8JsonWriter w, Context context, BelemetryAttributes resource, Source source, string eventId, bool browser, string sdkName, string sdkVersion)
    {
        var a = source.Attributes;
        var exceptionMessage = a.GetString(BelemetrySemanticConventions.ExceptionMessage);

        w.WriteStartObject();
        w.WriteString("event_id", eventId);
        w.WriteNumber("timestamp", source.TimeUnixNano / 1_000_000 / 1000.0);
        // The frames' platform: a .NET exception the browser script recorded (TrackException on a Blazor Server
        // circuit) has .NET frames.
        w.WriteString("platform", browser && SentryStackTrace.IsDotnet(a.GetString(BelemetrySemanticConventions.ExceptionStacktrace)) is false ? "javascript" : "csharp");
        w.WriteString("level", Level(source.Severity));
        if (string.IsNullOrEmpty(source.Scope) is false) w.WriteString("logger", source.Scope);
        if (source.Transaction is { Length: > 0 } transaction) w.WriteString("transaction", transaction);
        if (ReleaseOf(context, resource) is { } release) w.WriteString("release", release);
        if (resource.GetString(BelemetrySemanticConventions.DeploymentEnvironmentName) is { Length: > 0 } environment) w.WriteString("environment", environment);
        if (resource.GetString(BelemetrySemanticConventions.HostName) is { Length: > 0 } host) w.WriteString("server_name", host);

        // The log message, when it says more than the exception's own message (a TrackException's body is that message).
        if (string.IsNullOrEmpty(source.Body) is false && source.Body != exceptionMessage)
        {
            w.WriteStartObject("message");
            w.WriteString("formatted", source.Body);
            w.WriteEndObject();
        }

        List<SentryDebugImage> images = [];
        if (a.HasException) WriteExceptions(w, a, source, browser, images);

        WriteContexts(w, resource, source);

        if (source.Ids.GetString(BelemetrySemanticConventions.SessionId) is { Length: > 0 } session)
        {
            w.WriteStartObject("tags");
            w.WriteString(BelemetrySemanticConventions.SessionId, session);
            w.WriteEndObject();
        }

        var userId = source.Ids.GetString(BelemetrySemanticConventions.UserId);
        var pseudoId = source.Ids.GetString(BelemetrySemanticConventions.EndUserPseudoId);
        if (string.IsNullOrEmpty(userId) is false || string.IsNullOrEmpty(pseudoId) is false)
        {
            w.WriteStartObject("user");
            w.WriteString("id", string.IsNullOrEmpty(userId) ? pseudoId : userId);
            w.WriteEndObject();
        }

        if (resource.GetString(BelemetrySemanticConventions.UserAgentOriginal) is { Length: > 0 } userAgent)
        {
            // Sentry tells the browser and the operating system from it.
            w.WriteStartObject("request");
            w.WriteStartObject("headers");
            w.WriteString("User-Agent", userAgent);
            w.WriteEndObject();
            w.WriteEndObject();
        }

        WriteExtra(w, a, source, userId, pseudoId);

        w.WriteStartObject("sdk");
        w.WriteString("name", sdkName);
        w.WriteString("version", sdkVersion);
        w.WriteStartObject("settings");
        w.WriteString("infer_ip", context.MaskIp ? "never" : "auto");
        w.WriteEndObject();
        w.WriteEndObject();

        if (images.Count > 0)
        {
            w.WriteStartObject("debug_meta");
            w.WriteStartArray("images");
            foreach (var image in images)
            {
                w.WriteStartObject();
                w.WriteString("type", "pe_dotnet");
                w.WriteString("code_file", image.CodeFile);
                if (image.CodeId is not null) w.WriteString("code_id", image.CodeId);
                w.WriteString("debug_id", image.DebugId);
                if (image.DebugFile is not null) w.WriteString("debug_file", image.DebugFile);
                if (image.DebugChecksum is not null) w.WriteString("debug_checksum", image.DebugChecksum);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WriteEndObject();
    }

    /// <summary>The Sentry level of a severity.</summary>
    internal static string Level(BelemetrySeverity severity) => (int)severity switch
    {
        >= 21 => "fatal",
        >= 17 => "error",
        >= 13 => "warning",
        >= 9 => "info",
        _ => "debug",
    };

    /// <summary>The configured release, else <c>{service.name}@{service.version}</c> of the resource; null without a version.</summary>
    internal static string? ReleaseOf(Context context, BelemetryAttributes resource)
    {
        if (string.IsNullOrEmpty(context.Release) is false) return context.Release;
        var version = resource.GetString(BelemetrySemanticConventions.ServiceVersion);
        if (string.IsNullOrEmpty(version)) return null;
        var name = resource.GetString(BelemetrySemanticConventions.ServiceName);
        return string.IsNullOrEmpty(name) ? version : name + "@" + version;
    }

    /// <summary>
    /// Whether nothing caught the exception: the browser's <c>error</c> and <c>unhandledrejection</c>, Belemetry's own
    /// unhandled .NET exceptions, and the framework's renderers and hosts that log what the app left unhandled.
    /// </summary>
    internal static bool IsUnhandled(string scope, string? type) =>
        scope == BrowserErrorsScope || BelemetryDiagnosticEventFactory.OriginOf(scope, type) == "unhandled";

    private sealed record ExceptionValue(string? Type, string? Value, List<SentryStackTrace.Frame> Frames);

    private static void WriteExceptions(Utf8JsonWriter w, BelemetryAttributes a, Source source, bool browser, List<SentryDebugImage> images)
    {
        var type = a.GetString(BelemetrySemanticConventions.ExceptionType);
        var message = a.GetString(BelemetrySemanticConventions.ExceptionMessage);
        var stack = a.GetString(BelemetrySemanticConventions.ExceptionStacktrace);

        // Outermost first. The record's attributes are what privacy and the processors left: the outer exception's type
        // and message are theirs, and a stack trace they removed is not sent - not even from the exception object.
        var chain = new List<ExceptionValue>();
        if (stack is null)
        {
            chain.Add(new(type, message, []));
        }
        else if (browser && SentryStackTrace.IsDotnet(stack) is false)
        {
            chain.Add(new(type, message, SentryStackTrace.ParseJavaScript(stack)));
        }
        else
        {
            if (source.Exception is { } exception) AddFromObject(chain, exception, type, message, stack, images);

            // NativeAOT has no method of a frame: the text tells what the runtime printed.
            if (chain.Count == 0 || chain[0].Frames.Count == 0)
            {
                chain.Clear();
                images.Clear();
                var parsed = SentryStackTrace.ParseDotnet(stack);
                chain.Add(new(type ?? (parsed.Count > 0 ? parsed[0].Type : null), message, parsed.Count > 0 ? parsed[0].Frames : []));
                for (var i = 1; i < parsed.Count; i++) chain.Add(new(parsed[i].Type, parsed[i].Message, parsed[i].Frames));
            }
        }

        var handled = IsUnhandled(source.Scope, type) is false;

        // Sentry orders a chain oldest first: the innermost exception (the cause) first, the outermost last.
        w.WriteStartObject("exception");
        w.WriteStartArray("values");
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var value = chain[i];
            w.WriteStartObject();
            if (string.IsNullOrEmpty(value.Type) is false) w.WriteString("type", value.Type);
            if (string.IsNullOrEmpty(value.Value) is false) w.WriteString("value", value.Value);
            if (value.Frames.Count > 0)
            {
                w.WriteStartObject("stacktrace");
                w.WriteStartArray("frames");
                foreach (var frame in value.Frames) WriteFrame(w, frame);
                w.WriteEndArray();
                w.WriteEndObject();
            }
            if (i == 0)
            {
                w.WriteStartObject("mechanism");
                w.WriteString("type", "generic");
                w.WriteBoolean("handled", handled);
                w.WriteEndObject();
            }
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WriteEndObject();
    }

    /// <summary>
    /// The chain from the exception object, for its frames' method tokens and IL offsets. An inner exception's message is
    /// told only when the record's stack text still holds it as it is (the privacy rules left it alone).
    /// </summary>
    private static void AddFromObject(List<ExceptionValue> chain, Exception exception, string? type, string? message, string stack, List<SentryDebugImage> images)
    {
        var current = exception;
        while (current is not null && chain.Count < SentryStackTrace.MaxExceptions)
        {
            var frames = SentryStackTrace.FromException(current, images);
            if (chain.Count == 0)
            {
                chain.Add(new(type ?? current.GetType().FullName, message, frames));
            }
            else
            {
                var innerMessage = current.Message.Length > 0 && stack.Contains(current.Message, StringComparison.Ordinal) ? current.Message : null;
                chain.Add(new(current.GetType().FullName, innerMessage, frames));
            }
            current = current.InnerException;
        }
    }

    private static void WriteFrame(Utf8JsonWriter w, SentryStackTrace.Frame frame)
    {
        w.WriteStartObject();
        if (frame.Function is not null) w.WriteString("function", frame.Function);
        if (frame.Module is not null) w.WriteString("module", frame.Module);
        if (frame.FileName is not null) w.WriteString("filename", frame.FileName);
        if (frame.AbsolutePath is not null) w.WriteString("abs_path", frame.AbsolutePath);
        if (frame.Line is { } line) w.WriteNumber("lineno", line);
        if (frame.Column is { } column) w.WriteNumber("colno", column);
        w.WriteBoolean("in_app", frame.InApp);
        if (frame.FunctionId is not null)
        {
            w.WriteString("instruction_addr", frame.InstructionAddress);
            w.WriteString("function_id", frame.FunctionId);
            w.WriteString("addr_mode", "rel:" + frame.Image!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        w.WriteEndObject();
    }

    private static void WriteContexts(Utf8JsonWriter w, BelemetryAttributes resource, Source source)
    {
        w.WriteStartObject("contexts");

        // The trace the record was produced in: Sentry links the Issue to it.
        if (IsHex(source.TraceId, 32) && IsHex(source.SpanId, 16))
        {
            w.WriteStartObject("trace");
            w.WriteString("trace_id", source.TraceId);
            w.WriteString("span_id", source.SpanId);
            if (IsHex(source.ParentSpanId, 16)) w.WriteString("parent_span_id", source.ParentSpanId);
            w.WriteString("type", "trace");
            w.WriteEndObject();
        }

        var runtimeName = resource.GetString(BelemetrySemanticConventions.ProcessRuntimeName);
        var runtimeVersion = resource.GetString(BelemetrySemanticConventions.ProcessRuntimeVersion);
        var runtimeDescription = resource.GetString(BelemetrySemanticConventions.ProcessRuntimeDescription);
        if (runtimeName is not null || runtimeVersion is not null || runtimeDescription is not null)
        {
            w.WriteStartObject("runtime");
            if (runtimeName is not null) w.WriteString("name", runtimeName);
            if (runtimeVersion is not null) w.WriteString("version", runtimeVersion);
            if (runtimeDescription is not null) w.WriteString("raw_description", runtimeDescription);
            w.WriteString("type", "runtime");
            w.WriteEndObject();
        }

        var osType = resource.GetString(BelemetrySemanticConventions.OsType);
        var osDescription = resource.GetString(BelemetrySemanticConventions.OsDescription);
        if (osType is not null || osDescription is not null)
        {
            w.WriteStartObject("os");
            if (osType is not null) w.WriteString("name", osType);
            if (osDescription is not null) w.WriteString("raw_description", osDescription);
            w.WriteString("type", "os");
            w.WriteEndObject();
        }

        if (resource.GetString(BelemetrySemanticConventions.BrowserLanguage) is { Length: > 0 } locale)
        {
            w.WriteStartObject("culture");
            w.WriteString("locale", locale);
            w.WriteString("type", "culture");
            w.WriteEndObject();
        }

        // The whole resource, as Sentry's OpenTelemetry integration shows it: the service, its instance, the host.
        if (resource.Count > 0)
        {
            w.WriteStartObject("otel");
            w.WriteStartObject("resource");
            foreach (var (key, value) in resource) WriteValue(w, key, value);
            w.WriteEndObject();
            w.WriteString("type", "otel");
            w.WriteEndObject();
        }

        w.WriteEndObject();
    }

    /// <summary>The record's attributes the event's own fields do not hold.</summary>
    private static void WriteExtra(Utf8JsonWriter w, BelemetryAttributes a, Source source, string? userId, string? pseudoId)
    {
        var started = false;
        foreach (var (key, value) in a)
        {
            if (key is BelemetrySemanticConventions.ExceptionType or BelemetrySemanticConventions.ExceptionMessage or BelemetrySemanticConventions.ExceptionStacktrace) continue;

            // The ids are the event's tags and user when they are the record's own; the user id names the user, and the
            // pseudo id stays here beside it.
            if (ReferenceEquals(a, source.Ids))
            {
                if (key == BelemetrySemanticConventions.SessionId) continue;
                if (key == BelemetrySemanticConventions.UserId && string.IsNullOrEmpty(userId) is false) continue;
                if (key == BelemetrySemanticConventions.EndUserPseudoId && string.IsNullOrEmpty(userId)) continue;
            }

            if (started is false)
            {
                w.WriteStartObject("extra");
                started = true;
            }
            WriteValue(w, key, value);
        }

        if (started) w.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter w, string key, object value)
    {
        switch (value)
        {
            case string s: w.WriteString(key, s); break;
            case bool b: w.WriteBoolean(key, b); break;
            case long l: w.WriteNumber(key, l); break;
            case double d when double.IsFinite(d): w.WriteNumber(key, d); break;
            case string[] strings:
                w.WriteStartArray(key);
                foreach (var item in strings) w.WriteStringValue(item);
                w.WriteEndArray();
                break;
            case long[] longs:
                w.WriteStartArray(key);
                foreach (var item in longs) w.WriteNumberValue(item);
                w.WriteEndArray();
                break;
            case double[] doubles:
                w.WriteStartArray(key);
                foreach (var item in doubles)
                {
                    if (double.IsFinite(item)) w.WriteNumberValue(item);
                    else w.WriteNullValue();
                }
                w.WriteEndArray();
                break;
            case bool[] bools:
                w.WriteStartArray(key);
                foreach (var item in bools) w.WriteBooleanValue(item);
                w.WriteEndArray();
                break;
            default: w.WriteString(key, BelemetryAttributes.ToInvariantString(value)); break;
        }
    }

    private static bool IsHex(string? value, int length)
    {
        if (value is null || value.Length != length) return false;
        foreach (var c in value)
        {
            if (char.IsAsciiHexDigit(c) is false) return false;
        }
        return true;
    }
}
