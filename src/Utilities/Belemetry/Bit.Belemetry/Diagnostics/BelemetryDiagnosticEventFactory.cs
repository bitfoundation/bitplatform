using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bit.Belemetry;

/// <summary>
/// Turns what the pipeline and the framework report - log records, spans, activities - into diagnostic events: only
/// the metadata a category needs and the attributes the app allowlisted, through <see cref="BelemetryDiagnosticSanitizer"/>.
/// </summary>
internal sealed class BelemetryDiagnosticEventFactory
{
    internal const string ComponentsSource = "Microsoft.AspNetCore.Components";
    internal const string CircuitsSource = "Microsoft.AspNetCore.Components.Server.Circuits";
    internal const string NavigateActivity = "Microsoft.AspNetCore.Components.Navigate";
    internal const string HandleEventActivity = "Microsoft.AspNetCore.Components.HandleEvent";
    internal const string StartCircuitActivity = "Microsoft.AspNetCore.Components.Server.Circuits.StartCircuit";
    internal const string ComponentTypeTag = "aspnetcore.components.type";
    internal const string RouteTag = "aspnetcore.components.route";
    internal const string AttributeNameTag = "aspnetcore.components.attribute.name";
    internal const string MethodTag = "code.function.name";
    internal const string ApiScope = "Bit.Belemetry.Api";

    private const int MaxInnerDepth = 3;
    private const int MaxAggregateInner = 3;

    // The attributes each category keeps: metadata, never values.
    private static readonly string[] _httpKeys = [BelemetrySemanticConventions.HttpRequestMethod, BelemetrySemanticConventions.HttpResponseStatusCode, BelemetrySemanticConventions.ServerAddress, BelemetrySemanticConventions.ServerPort, BelemetrySemanticConventions.ErrorType, BelemetrySemanticConventions.HttpRoute, "network.protocol.name"];
    private static readonly string[] _componentKeys = [MethodTag, AttributeNameTag, BelemetrySemanticConventions.ErrorType];
    private static readonly string[] _traceKeys = [BelemetrySemanticConventions.ErrorType, BelemetrySemanticConventions.DbSystemName, "db.operation.name", "messaging.system", "rpc.system"];
    private static readonly string[] _logKeys = [BelemetrySemanticConventions.LogEventId, BelemetrySemanticConventions.LogEventName, BelemetrySemanticConventions.ErrorType];

    private readonly BelemetrySettings _settings;
    private readonly BelemetryDiagnosticSanitizer _sanitizer;
    private readonly IBelemetryExceptionClassifier[] _classifiers;

    // The logged exception objects and the occurrence each was recorded as. Weak: the table keeps no exception alive.
    private readonly ConditionalWeakTable<Exception, StrongBox<long>> _occurrences = new();
    private long _lastOccurrence;

    public BelemetryDiagnosticEventFactory(BelemetrySettings settings, BelemetryDiagnosticSanitizer sanitizer, IEnumerable<IBelemetryExceptionClassifier>? classifiers)
    {
        _settings = settings;
        _sanitizer = sanitizer;
        _classifiers = classifiers?.ToArray() ?? [];
    }

    private BelemetryDiagnosticsOptions Options => _sanitizer.Options;

    /// <summary>The release occurrences are attributed to: the service version.</summary>
    public string? Release => _settings.ServiceVersion;

    /// <summary>
    /// The occurrence a logged exception object was last recorded as (<see cref="BelemetryDiagnosticEvent.OccurrenceId"/>);
    /// null when it was never logged here - or was dropped before (the error limits).
    /// </summary>
    public long? OccurrenceOf(Exception exception) => _occurrences.TryGetValue(exception, out var occurrence) ? occurrence.Value : null;

    /// <summary>A log record; null when its category is switched off or it is filtered out.</summary>
    public BelemetryDiagnosticEvent? FromLog(BelemetryLogRecord log, BelemetryDiagnosticRecorder recorder)
    {
        var counters = new BelemetrySanitizeCounters();
        var scope = log.Scope.Name;
        BelemetryDiagnosticException? exception = null;

        if (log.Exception is not null || log.HasException)
        {
            var origin = OriginOf(scope, log.Attributes.GetString(BelemetrySemanticConventions.ExceptionType) ?? log.Exception?.GetType().FullName);
            exception = log.Exception is { } ex
                ? FromRecordedException(ex, log.Attributes, origin, ref counters)
                : FromText(log.Attributes.GetString(BelemetrySemanticConventions.ExceptionType), log.Attributes.GetString(BelemetrySemanticConventions.ExceptionMessage),
                           log.Attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace), origin, ref counters);
            if (exception is null) return null; // Ignored by a classifier.
        }

        // The category says what failed (a JS interop call), the origin who reported it (the renderer, unhandled).
        var category = exception is null ? BelemetryDiagnosticCategories.Log
                     : exception.Type.StartsWith("Microsoft.JSInterop.", StringComparison.Ordinal) ? BelemetryDiagnosticCategories.JsInterop
                     : exception.Origin == "circuit" ? BelemetryDiagnosticCategories.Circuit
                     : BelemetryDiagnosticCategories.Exception;
        if (exception is null && IsCircuitScope(scope)) category = BelemetryDiagnosticCategories.Circuit;
        if ((Options.Categories & category) == 0) return null;

        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal) { [BelemetrySemanticConventions.LogCategory] = _sanitizer.Text(scope, 256, ref counters) };
        Keep(log.Attributes, _logKeys, attributes, ref counters);

        // The template rather than the rendered message, unless the app asked for messages: "Order {OrderId} failed". A
        // message that renders a value whose key diagnostics redact (a circuit id) falls back to its template too.
        string? message;
        if (Options.IncludeLogMessages && RendersRedactedValue(log) is false) message = _sanitizer.Text(log.Body, 2048, ref counters);
        else
        {
            message = _sanitizer.Text(log.MessageTemplate, 1024, ref counters);
            if (message is null && log.Body is not null)
            {
                counters.Omissions++;
                attributes["log.message_omitted"] = true;
            }
        }

        long occurrence = 0;
        if (exception is not null && log.Exception is { } thrown)
        {
            occurrence = Interlocked.Increment(ref _lastOccurrence);
            _occurrences.AddOrUpdate(thrown, new StrongBox<long>(occurrence));
        }

        return new BelemetryDiagnosticEvent
        {
            Sequence = recorder.NextSequence(),
            TimeUnixNano = log.TimeUnixNano,
            ElapsedMilliseconds = recorder.ElapsedMilliseconds(),
            Category = category,
            Name = exception is null ? "log" : "exception",
            Severity = log.Severity == BelemetrySeverity.Unspecified ? BelemetrySeverity.Info : log.Severity,
            Outcome = exception is null ? BelemetryDiagnosticOutcome.Unknown : BelemetryDiagnosticOutcome.Failure,
            Source = scope.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ? BelemetryDiagnosticSource.Framework : BelemetryDiagnosticSource.Dotnet,
            Feature = log.Feature,
            TraceId = log.TraceId,
            SpanId = log.SpanId,
            ComponentType = exception is null ? null : InferComponent(exception),
            ComponentSource = exception is null || InferComponent(exception) is null ? null : "stack",
            Exception = exception,
            Message = message,
            Attributes = attributes,
            Redactions = counters.Redactions,
            Truncated = counters.Truncations > 0,
            OccurrenceId = occurrence,
        };
    }

    /// <summary>A span the pipeline accepted; null when it is not worth a timeline entry.</summary>
    public BelemetryDiagnosticEvent? FromSpan(BelemetrySpan span, BelemetryDiagnosticRecorder recorder)
    {
        var counters = new BelemetrySanitizeCounters();
        var outcome = OutcomeOf(span);
        var failed = outcome == BelemetryDiagnosticOutcome.Failure;
        if (failed is false && Options.IncludeSuccessfulActivity is false) return null;

        var method = span.Attributes.GetString(BelemetrySemanticConventions.HttpRequestMethod);
        var isHttp = method is not null && span.Kind is BelemetrySpanKind.Client or BelemetrySpanKind.Server;
        var category = isHttp ? BelemetryDiagnosticCategories.Http : BelemetryDiagnosticCategories.Trace;
        if ((Options.Categories & category) == 0) return null;

        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        Keep(span.Attributes, isHttp ? _httpKeys : _traceKeys, attributes, ref counters);
        string? route = null;
        if (isHttp)
        {
            if (_sanitizer.Url(span.Attributes.GetString(BelemetrySemanticConventions.UrlFull) ?? span.Attributes.GetString(BelemetrySemanticConventions.UrlPath), ref counters) is { } url) attributes["url.template"] = url;
            route = span.Kind == BelemetrySpanKind.Server ? span.Attributes.GetString(BelemetrySemanticConventions.HttpRoute) : null;
        }

        return new BelemetryDiagnosticEvent
        {
            Sequence = recorder.NextSequence(),
            TimeUnixNano = span.StartTimeUnixNano,
            ElapsedMilliseconds = recorder.ElapsedMilliseconds(),
            Category = category,
            Name = isHttp ? method! : _sanitizer.Text(span.Name, 128, ref counters) ?? "span",
            Severity = failed ? (IsClientError(span) ? BelemetrySeverity.Warn : BelemetrySeverity.Error) : BelemetrySeverity.Info,
            Outcome = outcome,
            Source = BelemetryDiagnosticSource.Dotnet,
            Feature = span.Feature,
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            ParentSpanId = span.ParentSpanId,
            Route = route,
            RouteSource = route is null ? null : "framework",
            DurationMilliseconds = span.Duration.TotalMilliseconds,
            Message = failed ? _sanitizer.Text(span.StatusMessage, 512, ref counters) : null,
            Attributes = attributes,
            Redactions = counters.Redactions,
            Truncated = counters.Truncations > 0,
        };
    }

    /// <summary>A stopped framework activity (<c>Navigate</c>, <c>HandleEvent</c>, <c>StartCircuit</c>).</summary>
    public BelemetryDiagnosticEvent? FromFrameworkActivity(Activity activity, BelemetryDiagnosticRecorder recorder)
    {
        var counters = new BelemetrySanitizeCounters();
        var errorType = activity.GetTagItem(BelemetrySemanticConventions.ErrorType) as string;
        var failed = activity.Status == ActivityStatusCode.Error || errorType is not null;
        if (failed is false && Options.IncludeSuccessfulActivity is false) return null;

        BelemetryDiagnosticCategories category;
        string name;
        switch (activity.OperationName)
        {
            case NavigateActivity:
                category = BelemetryDiagnosticCategories.Navigation;
                name = "navigate";
                break;
            case HandleEventActivity:
                category = BelemetryDiagnosticCategories.Component;
                name = "handle_event";
                break;
            case StartCircuitActivity:
                category = BelemetryDiagnosticCategories.Circuit;
                name = "circuit.start";
                break;
            default:
                return null;
        }
        if ((Options.Categories & category) == 0) return null;

        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var key in _componentKeys)
        {
            if (activity.GetTagItem(key) is { } value && _sanitizer.Scalar(key, value, ref counters, 256) is { } kept) attributes[key] = kept;
        }

        // Route templates come from the router ("/orders/{id}"); the value is never a raw URL.
        var route = activity.GetTagItem(RouteTag) as string;
        var component = activity.GetTagItem(ComponentTypeTag) as string;

        return new BelemetryDiagnosticEvent
        {
            Sequence = recorder.NextSequence(),
            TimeUnixNano = BelemetryTime.ToUnixNano(activity.StartTimeUtc),
            ElapsedMilliseconds = recorder.ElapsedMilliseconds(),
            Category = category,
            Name = name,
            Severity = failed ? BelemetrySeverity.Error : BelemetrySeverity.Info,
            Outcome = failed ? BelemetryDiagnosticOutcome.Failure : BelemetryDiagnosticOutcome.Success,
            Source = BelemetryDiagnosticSource.Framework,
            Feature = category == BelemetryDiagnosticCategories.Navigation ? BelemetryFeatures.PageViews : BelemetryFeatures.Traces,
            TraceId = activity.IdFormat == ActivityIdFormat.W3C ? activity.TraceId.ToHexString() : null,
            SpanId = activity.IdFormat == ActivityIdFormat.W3C ? activity.SpanId.ToHexString() : null,
            ParentSpanId = activity.IdFormat == ActivityIdFormat.W3C && activity.ParentSpanId != default ? activity.ParentSpanId.ToHexString() : null,
            Route = route is null ? null : _sanitizer.Text(route, 512, ref counters),
            RouteSource = route is null ? null : "framework",
            ComponentType = component is null ? null : _sanitizer.Text(component, 512, ref counters),
            ComponentSource = component is null ? null : "framework",
            DurationMilliseconds = activity.Duration.TotalMilliseconds,
            Message = failed ? _sanitizer.Text(errorType ?? activity.StatusDescription, 256, ref counters) : null,
            Attributes = attributes,
            Redactions = counters.Redactions,
            Truncated = counters.Truncations > 0,
        };
    }

    /// <summary>An event the app recorded itself.</summary>
    public BelemetryDiagnosticEvent Custom(string name, IReadOnlyDictionary<string, object?>? attributes, BelemetrySeverity severity, BelemetryDiagnosticCategories category, BelemetryDiagnosticRecorder recorder)
    {
        var counters = new BelemetrySanitizeCounters();
        var kept = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (attributes is not null)
        {
            // The app named these explicitly, so they are kept - through the same sanitizer as everything else.
            foreach (var (key, value) in attributes.Take(32))
            {
                if (_sanitizer.Scalar(key, value, ref counters) is { } v) kept[key.Length > 128 ? key[..128] : key] = v;
            }
        }

        var activity = Activity.Current;
        return new BelemetryDiagnosticEvent
        {
            Sequence = recorder.NextSequence(),
            TimeUnixNano = BelemetryTime.NowUnixNano(),
            ElapsedMilliseconds = recorder.ElapsedMilliseconds(),
            Category = category,
            Name = _sanitizer.Text(name, 128, ref counters) ?? "event",
            Severity = severity,
            Source = BelemetryDiagnosticSource.Application,
            Feature = BelemetryFeatures.None,
            TraceId = activity?.IdFormat == ActivityIdFormat.W3C ? activity.TraceId.ToHexString() : null,
            SpanId = activity?.IdFormat == ActivityIdFormat.W3C ? activity.SpanId.ToHexString() : null,
            Attributes = kept,
            Redactions = counters.Redactions,
            Truncated = counters.Truncations > 0,
        };
    }

    /// <summary>An exception object: its type, sanitized message and template, stack without paths, inner exceptions, fingerprint and classification.</summary>
    public BelemetryDiagnosticException? FromException(Exception exception, string origin, ref BelemetrySanitizeCounters counters, int depth = 0)
    {
        var type = exception.GetType().FullName ?? exception.GetType().Name;
        var inner = new List<BelemetryDiagnosticException>();
        if (depth < MaxInnerDepth)
        {
            if (exception is AggregateException aggregate)
            {
                foreach (var item in aggregate.InnerExceptions.Take(MaxAggregateInner))
                {
                    if (FromException(item, origin, ref counters, depth + 1) is { } converted) inner.Add(converted);
                }
            }
            else if (exception.InnerException is { } innerException && FromException(innerException, origin, ref counters, depth + 1) is { } converted)
            {
                inner.Add(converted);
            }
        }

        return Build(type, exception.Message, exception.StackTrace, origin, inner, ref counters, classify: depth == 0);
    }

    /// <summary>
    /// An exception a log record carries, as the export holds it: its type, message and stack from the record's attributes -
    /// after the privacy rules and the app's processors, so a blocked or rewritten message stays blocked or rewritten here -
    /// and only its structure (the inner exceptions' types) from the object. An inner exception's message and frames are
    /// kept only when the record's stack text still holds them.
    /// </summary>
    private BelemetryDiagnosticException? FromRecordedException(Exception exception, BelemetryAttributes attributes, string origin, ref BelemetrySanitizeCounters counters)
    {
        var type = attributes.GetString(BelemetrySemanticConventions.ExceptionType) ?? exception.GetType().FullName ?? exception.GetType().Name;
        var stackText = attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace);
        var inner = new List<BelemetryDiagnosticException>();
        foreach (var item in InnerOf(exception))
        {
            if (Inner(item, stackText, origin, ref counters, depth: 1) is { } converted) inner.Add(converted);
        }

        return Build(type, attributes.GetString(BelemetrySemanticConventions.ExceptionMessage), OuterStack(stackText), origin, inner, ref counters, classify: true);
    }

    private BelemetryDiagnosticException? Inner(Exception exception, string? stackText, string origin, ref BelemetrySanitizeCounters counters, int depth)
    {
        var inner = new List<BelemetryDiagnosticException>();
        if (depth < MaxInnerDepth)
        {
            foreach (var item in InnerOf(exception))
            {
                if (Inner(item, stackText, origin, ref counters, depth + 1) is { } converted) inner.Add(converted);
            }
        }

        // Held by the record's stack text as the export holds it: raw, or scrubbed by the privacy rules the same way.
        var message = exception.Message.Length > 0 && stackText is not null
                      && (stackText.Contains(exception.Message, StringComparison.Ordinal)
                          || (_sanitizer.Text(exception.Message, 2048, ref counters) is { } scrubbed && stackText.Contains(scrubbed, StringComparison.Ordinal)))
            ? exception.Message
            : null;
        return Build(exception.GetType().FullName ?? exception.GetType().Name, message, stackText is null ? null : exception.StackTrace, origin, inner, ref counters, classify: false);
    }

    private static IEnumerable<Exception> InnerOf(Exception exception) =>
        exception is AggregateException aggregate ? aggregate.InnerExceptions.Take(MaxAggregateInner)
        : exception.InnerException is { } inner ? [inner]
        : [];

    /// <summary>
    /// A .NET exception's text (exception.ToString()) starts with the inner exceptions' frames: only the frames after the
    /// last "End of inner exception stack trace" marker are the outer exception's.
    /// </summary>
    private static string? OuterStack(string? stackTrace)
    {
        if (stackTrace is null) return null;
        const string marker = "--- End of inner exception stack trace ---";
        var last = stackTrace.LastIndexOf(marker, StringComparison.Ordinal);
        return last >= 0 ? stackTrace[(last + marker.Length)..] : stackTrace;
    }

    /// <summary>
    /// A stack text without its message lines: a .NET exception's text (<c>exception.ToString()</c>) and a JavaScript
    /// stack start with <c>Type: message</c>, and an inner exception adds <c>---&gt; Type: message</c>. Only the frames and
    /// the runtime's <c>--- End of ... ---</c> markers stay.
    /// </summary>
    private static string? FramesOnly(string? stack)
    {
        if (stack is null) return null;

        var builder = new System.Text.StringBuilder(stack.Length);
        foreach (var line in stack.AsSpan().EnumerateLines())
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("--- ", StringComparison.Ordinal) is false && BelemetryExceptionFingerprint.NormalizeFrame(trimmed.ToString()) is null) continue;
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line);
        }
        return builder.Length == 0 ? null : builder.ToString();
    }

    private bool RendersRedactedValue(BelemetryLogRecord log)
    {
        foreach (var (key, value) in log.Attributes)
        {
            if (value is not null && _sanitizer.IsRedactedKey(key)) return true;
        }
        return false;
    }

    /// <summary>An exception known only as text (a relayed or browser record, or one whose object is gone).</summary>
    public BelemetryDiagnosticException? FromText(string? type, string? message, string? stackTrace, string origin, ref BelemetrySanitizeCounters counters)
    {
        return Build(type ?? "Error", message, OuterStack(stackTrace), origin, [], ref counters, classify: true);
    }

    private BelemetryDiagnosticException? Build(string type, string? rawMessage, string? rawStack, string origin, List<BelemetryDiagnosticException> inner, ref BelemetrySanitizeCounters counters, bool classify)
    {
        type = BelemetryExceptionFingerprint.NormalizeType(type);

        // Scrubbed before anything is derived from it: the template is kept even when the message is not.
        var scrubbed = _sanitizer.Text(rawMessage, 2048, ref counters);
        var stack = rawStack is null ? null : _sanitizer.Text(BelemetryExceptionFingerprint.StripPaths(rawStack), 8192, ref counters);
        var release = Options.GroupAcrossReleases ? null : Release;

        // The stack is parsed once: the frames make the fingerprint, the anchor and whether quoted values stay in the template.
        var frames = BelemetryExceptionFingerprint.MeaningfulFrames(stack);
        var rawTemplate = BelemetryExceptionFingerprint.TemplateFor(scrubbed, frames);
        var template = _sanitizer.Text(rawTemplate, 512, ref counters);

        var exception = new BelemetryDiagnosticException
        {
            Type = type,
            // Without messages, their templates go too: with no stack, a template keeps quoted text whole. It still makes
            // the fingerprint, which is a hash.
            Message = Options.IncludeExceptionMessages ? scrubbed : null,
            MessageTemplate = Options.IncludeExceptionMessages ? template : null,
            // The frames were read from the whole text above, so the fingerprint does not change with the option.
            StackTrace = Options.IncludeExceptionMessages ? stack : FramesOnly(stack),
            Fingerprint = BelemetryExceptionFingerprint.FromParts(type, rawTemplate, frames, classification: null, release),
            Anchor = frames.Count > 0 ? frames[0] : null,
            Origin = origin,
            Handled = origin switch { "error-boundary" or "tracked" => true, "unhandled" => false, _ => null },
            InnerExceptions = inner,
        };

        if (classify is false || _classifiers.Length == 0) return exception;

        foreach (var classifier in _classifiers)
        {
            BelemetryExceptionClassification? classification;
            try
            {
                classification = classifier.Classify(exception);
            }
            catch
            {
                continue;
            }

            if (classification is null) continue;
            if (classification.Ignore) return null;

            var custom = string.IsNullOrEmpty(classification.Fingerprint) is false;
            var category = classification.Category is null ? null : _sanitizer.Text(classification.Category, 64, ref counters);
            return new BelemetryDiagnosticException
            {
                Type = exception.Type,
                Message = exception.Message,
                MessageTemplate = exception.MessageTemplate,
                StackTrace = exception.StackTrace,
                Fingerprint = custom ? BelemetryExceptionFingerprint.FromKey(classification.Fingerprint!) : BelemetryExceptionFingerprint.FromParts(type, rawTemplate, frames, category, release),
                CustomFingerprint = custom,
                Anchor = exception.Anchor,
                Origin = exception.Origin,
                Handled = exception.Handled,
                Classification = category,
                Critical = classification.Critical,
                InnerExceptions = exception.InnerExceptions,
            };
        }

        return exception;
    }

    /// <summary>
    /// Where an exception logged by <paramref name="scope"/> was observed. The categories are the framework's own logger
    /// categories (.NET 10): the renderers and the circuit host log what nothing handled, <c>ErrorBoundary</c> what one caught.
    /// Only the framework's own categories mean "unhandled": an app's <c>PdfRenderer</c> logs what it handled.
    /// </summary>
    internal static string OriginOf(string scope, string? exceptionType)
    {
        // Who reported it first: an unhandled JSException is unhandled (it ends a circuit) before it is an interop failure.
        if (scope.Contains("ErrorBoundary", StringComparison.Ordinal)) return "error-boundary";
        if (scope == ApiScope) return "tracked";
        if (IsFrameworkRendererScope(scope)
            || (IsCircuitScope(scope) && scope.EndsWith(".CircuitHost", StringComparison.Ordinal))
            || scope == "Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHost") return "unhandled";
        if (IsCircuitScope(scope)) return "circuit";
        if (scope.StartsWith("Microsoft.JSInterop", StringComparison.Ordinal)) return "js-interop";
        if (exceptionType is not null && exceptionType.StartsWith("Microsoft.JSInterop.", StringComparison.Ordinal)) return "js-interop";
        return "log";
    }

    /// <summary>
    /// A renderer of the framework's (<c>RenderTree.Renderer</c>, <c>RemoteRenderer</c>, <c>WebAssemblyRenderer</c>,
    /// <c>WebViewRenderer</c>, <c>EndpointHtmlRenderer</c>, ...), never one of the app's.
    /// </summary>
    private static bool IsFrameworkRendererScope(string scope) =>
        scope.StartsWith("Microsoft.AspNetCore.Components.", StringComparison.Ordinal) && scope.EndsWith("Renderer", StringComparison.Ordinal);

    internal static bool IsCircuitScope(string scope) =>
        scope.StartsWith("Microsoft.AspNetCore.Components.Server.Circuits", StringComparison.Ordinal)
        || scope.StartsWith("Microsoft.AspNetCore.Components.Server.ComponentHub", StringComparison.Ordinal);

    /// <summary>
    /// The component an exception's first application frame belongs to - an inference, marked as such
    /// (<see cref="BelemetryDiagnosticEvent.ComponentSource"/> <c>stack</c>): the frame's type, with a nested or
    /// compiler-generated type reduced to its declaring type.
    /// </summary>
    internal static string? InferComponent(BelemetryDiagnosticException exception)
    {
        var anchor = exception.Anchor;
        if (anchor is null || anchor.Contains('@') || BelemetryExceptionFingerprint.IsFrameworkFrame(anchor)) return null;

        var lastDot = anchor.LastIndexOf('.');
        if (lastDot <= 0) return null;
        var type = anchor[..lastDot];
        var plus = type.IndexOf('+');
        return plus > 0 ? type[..plus] : type;
    }

    private void Keep(BelemetryAttributes source, string[] keys, Dictionary<string, object?> target, ref BelemetrySanitizeCounters counters)
    {
        foreach (var key in keys)
        {
            if (source.TryGetValue(key, out var value) && _sanitizer.Scalar(key, value, ref counters, 256) is { } kept) target[key] = kept;
        }

        if (Options.AllowedAttributes.Count == 0) return;
        foreach (var (key, value) in source)
        {
            if (target.Count >= 32) break;
            if (_sanitizer.IsAllowedAttribute(key) && target.ContainsKey(key) is false && _sanitizer.Scalar(key, value, ref counters) is { } kept) target[key] = kept;
        }
    }

    private static bool IsClientError(BelemetrySpan span)
    {
        var status = span.Attributes.GetInt64(BelemetrySemanticConventions.HttpResponseStatusCode);
        return status is >= 400 and < 500;
    }

    /// <summary>Whether a span failed, as <see cref="FromSpan"/> decides it - without converting it.</summary>
    internal static bool IsFailure(BelemetrySpan span) => OutcomeOf(span) == BelemetryDiagnosticOutcome.Failure;

    private static BelemetryDiagnosticOutcome OutcomeOf(BelemetrySpan span) => OutcomeOf(span.Status, span.Attributes.GetString(BelemetrySemanticConventions.ErrorType));

    /// <summary>
    /// How a span ended. A cancellation first: HttpClient marks a cancelled request with an Error status and the
    /// cancellation's type, and a request the app gave up on did not fail.
    /// </summary>
    internal static BelemetryDiagnosticOutcome OutcomeOf(BelemetryStatusCode status, string? errorType) =>
        errorType is "AbortError" or "System.Threading.Tasks.TaskCanceledException" or "System.OperationCanceledException" ? BelemetryDiagnosticOutcome.Cancelled
        : status == BelemetryStatusCode.Error ? BelemetryDiagnosticOutcome.Failure
        : BelemetryDiagnosticOutcome.Success;
}
