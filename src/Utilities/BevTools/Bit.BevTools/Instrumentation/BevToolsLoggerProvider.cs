using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// Captures what the application's loggers write while a session's code runs (Logs panel), and the warnings and errors
/// Blazor itself logs - unhandled circuit exceptions, event handler failures, ErrorBoundary catches, JS interop failures,
/// SignalR problems - into the error center of the session that produced them. An application entry at Error or above
/// is an error too: an exception the app caught and logged is still a failure. Entries written outside every session
/// (the request pipeline of a server, a hosted service) are not attributed to one, whichever circuit is connected.
/// <para>
/// Filtering by the host's logging configuration applies first, as to every provider; its alias is <c>BevTools</c>, so
/// <c>Logging:BevTools:LogLevel</c> sets what reaches BevTools alone. <see cref="BitBevToolsOptions.MinimumLogLevel"/>
/// applies after that.
/// </para>
/// </summary>
[ProviderAlias("BevTools")]
internal sealed class BevToolsLoggerProvider(BevToolsRegistry registry) : ILoggerProvider, ISupportExternalScope
{
    private const int MaxProperties = 20;
    // A message template's holes, the most read of one entry's state; far more than any template has.
    private const int MaxPairs = 128;
    private const int MaxScopes = 8;
    private const int MaxMessageLength = 4000;
    private const int MaxScopeLength = 300;

    private static readonly string[] FrameworkPrefixes =
    [
        "Microsoft.AspNetCore.Components",
        "Microsoft.JSInterop",
        "Microsoft.AspNetCore.SignalR",
        "Microsoft.AspNetCore.Http.Connections",
    ];

    // A value's ToString, a scope's or a formatter can log in turn; what BevTools' own capture causes is not the app's.
    [ThreadStatic]
    private static bool _capturing;

    private IExternalScopeProvider? _scopes;

    private BitBevToolsOptions Options => registry.Options;

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public ILogger CreateLogger(string categoryName)
    {
        if (!registry.IsEnabled || categoryName == "Bit.BevTools")
        {
            return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }

        var framework = IsFrameworkCategory(categoryName);
        return framework || registry.Options.TrackLogs
            ? new CaptureLogger(this, categoryName, framework)
            : Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    internal static bool IsFrameworkCategory(string category)
    {
        foreach (var prefix in FrameworkPrefixes)
        {
            // The namespace itself or one below it: "Microsoft.JSInteropExtras" is someone else's.
            if (category.StartsWith(prefix, StringComparison.Ordinal) && (category.Length == prefix.Length || category[prefix.Length] == '.'))
            {
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
    }

    private sealed class CaptureLogger(BevToolsLoggerProvider provider, string category, bool framework) : ILogger
    {
        // A category is code - the name CreateLogger was given, a type's most of the time - and is kept at every value
        // access, like a component's type; credential-shaped text in it is redacted all the same. Classified by the
        // name as given, shown as redacted.
        private readonly string _shownCategory = BevRedactor.RedactText(category);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => provider._scopes?.Push(state);

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None
            && ((framework && logLevel >= LogLevel.Warning)
                || (provider.Options.TrackLogs && logLevel >= provider.Options.MinimumLogLevel));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_capturing || !IsEnabled(logLevel))
            {
                return;
            }

            var session = SessionResolver.Resolve();
            if (session is null || !session.IsEnabled)
            {
                return;
            }

            _capturing = true;
            try
            {
                Capture(session, logLevel, eventId, state, exception, formatter);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // The application's other providers still log; a failure here must never reach the code that logged.
            }
            finally
            {
                _capturing = false;
            }
        }

        private void Capture<TState>(BevToolsSession session, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string? formatted = null;
            string Formatted() => formatted ??= Safe(() => formatter(state, exception)) ?? "";

            long? errorId = null;
            long errorEventId = 0;
            if (framework && logLevel >= LogLevel.Warning)
            {
                var message = BevRedactor.RedactText(Formatted());
                var source = category switch
                {
                    _ when category.Contains("Circuit", StringComparison.Ordinal) => "circuit",
                    _ when category.Contains("ErrorBoundary", StringComparison.Ordinal) => "error-boundary",
                    _ when category.StartsWith("Microsoft.JSInterop", StringComparison.Ordinal) => "js-interop",
                    _ when category.StartsWith("Microsoft.AspNetCore.SignalR", StringComparison.Ordinal) || category.Contains("Connections", StringComparison.Ordinal) => "signalr",
                    _ => "blazor",
                };

                if (exception is null && logLevel == LogLevel.Warning)
                {
                    errorEventId = session.Timeline.Record(new BevToolsEvent
                    {
                        Kind = BevToolsEventKind.Error,
                        Category = "log:" + source,
                        Title = message,
                        Detail = _shownCategory,
                        Severity = BevToolsSeverity.Warning,
                    });
                }
                else
                {
                    var error = session.Errors.Record(exception, source, message, exceptionType: exception?.GetType().FullName ?? "Log" + logLevel);
                    errorId = error.Id;
                    errorEventId = error.TimelineEventId;
                }
            }

            var options = session.Options;
            var appError = !framework && logLevel >= LogLevel.Error;
            if (!options.TrackLogs || logLevel < options.MinimumLogLevel || (!session.IsCapturing && !appError))
            {
                return;
            }

            var record = Read(session, logLevel, eventId, state, exception, Formatted);
            if (appError)
            {
                // Errors are collected while capture is paused, like every other error; the entry itself is not.
                var error = session.Errors.Record(exception, "log", ShortCategory(_shownCategory) + ": " + record.Text, exceptionType: exception?.GetType().FullName ?? "Log" + logLevel);
                errorId = error.Id;
                errorEventId = error.TimelineEventId;
            }

            if (!session.IsCapturing)
            {
                return;
            }

            record.ErrorId = errorId;
            record.TimelineEventId = errorEventId > 0 ? errorEventId : session.Timeline.Record(new BevToolsEvent
            {
                Kind = BevToolsEventKind.Log,
                Category = "log",
                Title = record.ShortCategory + ": " + Shorten(record.Text, 200),
                Detail = _shownCategory,
                Severity = logLevel switch
                {
                    >= LogLevel.Error => BevToolsSeverity.Error,
                    LogLevel.Warning => BevToolsSeverity.Warning,
                    _ => BevToolsSeverity.Info,
                },
                ParentEventId = record.TriggerEventId,
                Data = new Dictionary<string, object?>
                {
                    ["logId"] = record.Id,
                    ["level"] = logLevel.ToString(),
                    ["category"] = _shownCategory,
                    ["eventId"] = eventId.Id,
                },
            });
            session.Logs.Add(record);
        }

        private BevLogRecord Read<TState>(BevToolsSession session, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<string> formatted)
        {
            var inspector = session.Inspector;
            string? template = null;
            List<KeyValuePair<string, string>> properties = [];
            List<(string Name, object? Value, BevInspectedNode Node)>? values = null;
            var rebuild = inspector.ValueAccess != BevValueAccess.Full;
            if (state is IReadOnlyList<KeyValuePair<string, object?>> pairs)
            {
                var count = Math.Min(pairs.Count, MaxPairs);
                for (var i = 0; i < count; i++)
                {
                    var (name, value) = pairs[i];
                    if (name == "{OriginalFormat}")
                    {
                        template = value as string;
                        continue;
                    }

                    // Every value the message holds is inspected, so a sensitive one past the shown properties still
                    // rebuilds it; only the first ones are listed.
                    var node = inspector.Describe(name, value);
                    if (properties.Count < MaxProperties)
                    {
                        properties.Add(new(name, node.Display));
                    }

                    (values ??= []).Add((name, value, node));
                    // A value redacted by its name is in the logger's own text all the same: the message is rebuilt
                    // from what the properties show instead.
                    rebuild |= node.Kind == BevNodeKind.Redacted;
                }

                if (pairs.Count > count)
                {
                    // Not a template's state any more; what was not inspected stays out of the message, its hole as written.
                    rebuild = true;
                    if (pairs[^1] is { Key: "{OriginalFormat}", Value: string last })
                    {
                        template = last;
                    }
                }
            }

            // Code most of the time, but an interpolated or concatenated string logged as it is is its own template,
            // values and all: under None a template is kept only with holes, since without any nothing tells the two apart.
            template = template is null || (inspector.ValueAccess == BevValueAccess.None && values is null) ? null : BevRedactor.RedactText(template);

            string? message;
            if (inspector.ValueAccess == BevValueAccess.None)
            {
                message = null;
            }
            else if (rebuild && template is not null)
            {
                message = Render(template, values);
            }
            else
            {
                message = BevRedactor.RedactText(formatted());
            }

            var dbCommand = state is IReadOnlyList<KeyValuePair<string, object?>> commandPairs && BevDbCommands.IsCommand(category, eventId.Id)
                ? BevDbCommands.Read(eventId.Id, commandPairs, inspector.ValueAccess)
                : null;
            var caller = session.Options.CaptureLogCallers ? BevCallSites.Find(skipFrames: 0) : default;
            var activity = Activity.Current;
            var w3c = activity is { IdFormat: ActivityIdFormat.W3C };
            return new BevLogRecord
            {
                Id = session.Logs.NextId(),
                At = DateTimeOffset.UtcNow,
                Level = logLevel,
                Category = _shownCategory,
                EventId = eventId.Id,
                EventName = eventId.Name is { } eventName ? BevRedactor.RedactText(eventName) : null,
                Message = message is null ? null : Shorten(message, MaxMessageLength),
                MessageTemplate = template is null ? null : Shorten(template, MaxMessageLength),
                Properties = properties,
                Scopes = ReadScopes(inspector),
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception is null ? null : DescribeException(exception),
                TraceId = w3c ? activity!.TraceId.ToHexString() : null,
                SpanId = w3c ? activity!.SpanId.ToHexString() : null,
                TriggerEventId = session.CurrentTriggerEventId,
                CallerComponent = caller.Component,
                CallerVia = caller.Via,
                DbCommand = dbCommand,
            };
        }

        private List<string> ReadScopes(BevObjectInspector inspector)
        {
            List<string> scopes = [];
            provider._scopes?.ForEachScope(static (scope, state) =>
            {
                if (scope is null || state.Scopes.Count >= MaxScopes)
                {
                    return;
                }

                state.Scopes.Add(Shorten(DescribeScope(scope, state.Inspector), MaxScopeLength));
            }, (Scopes: scopes, Inspector: inspector));
            return scopes;
        }

        /// <summary>
        /// A scope as one line. Key/value scopes (the request scope of ASP.NET Core, a <c>Dictionary</c>) are read pair by
        /// pair through the inspector, so a sensitive name is redacted and <see cref="BevValueAccess.None"/> shows the keys
        /// only; anything else is its text, or its type under None.
        /// </summary>
        private static string DescribeScope(object scope, BevObjectInspector inspector)
        {
            if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                var sb = new StringBuilder();
                foreach (var (key, value) in pairs.Take(MaxProperties))
                {
                    if (key == "{OriginalFormat}")
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(key);
                    if (inspector.ValueAccess != BevValueAccess.None)
                    {
                        sb.Append('=').Append(Unquoted(inspector.Describe(key, value)));
                    }
                }

                return sb.ToString();
            }

            if (inspector.ValueAccess == BevValueAccess.None)
            {
                return scope is string ? "string" : TypeNames.Short(scope.GetType());
            }

            return scope is string text
                ? BevRedactor.RedactText(text)
                : inspector.ValueAccess == BevValueAccess.Full ? BevRedactor.RedactText(Safe(scope.ToString) ?? "") : TypeNames.Short(scope.GetType());
        }

        /// <summary>
        /// The template with each hole filled with what its property shows: a sensitive value redacted, an object under
        /// <see cref="BevValueAccess.Shallow"/> as its type. The n-th hole takes the n-th value when that one bears its
        /// name, as <c>LoggerMessage.Define</c> and <c>ILogger.Log*</c> fill them, a name repeated included; otherwise the
        /// first value of its name, as the source generator does, which lists each parameter once, in the method's order.
        /// </summary>
        private static string Render(string template, List<(string Name, object? Value, BevInspectedNode Node)>? values)
        {
            var sb = new StringBuilder(template.Length + 32);
            var hole = 0;
            var i = 0;
            while (i < template.Length)
            {
                var c = template[i];
                if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
                {
                    sb.Append(c);
                    i += 2;
                    continue;
                }

                var end = c == '{' ? template.IndexOf('}', i + 1) : -1;
                if (end < 0)
                {
                    sb.Append(c);
                    i++;
                    continue;
                }

                var text = template.AsSpan(i + 1, end - i - 1);
                var stop = text.IndexOfAny(',', ':');
                var name = (stop < 0 ? text : text[..stop]).Trim().TrimStart('@').TrimStart('$').ToString();
                var found = values is not null && hole < values.Count && values[hole].Name == name ? hole : values?.FindIndex(v => v.Name == name) ?? -1;
                hole++;
                if (found < 0)
                {
                    sb.Append(template, i, end - i + 1);
                }
                else
                {
                    var (_, value, node) = values![found];
                    var format = stop >= 0 && text[stop] == ':' ? text[(stop + 1)..].ToString() : null;
                    sb.Append(node.Kind is BevNodeKind.Primitive && format is not null && value is IFormattable formattable
                        ? Safe(() => formattable.ToString(format, CultureInfo.InvariantCulture)) ?? Unquoted(node)
                        : Unquoted(node));
                }

                i = end + 1;
            }

            return BevRedactor.RedactText(sb.ToString());
        }

        private static string Unquoted(BevInspectedNode node) =>
            node.Kind == BevNodeKind.String && node.Display.Length >= 2 && node.Display[0] == '"' && node.Display[^1] == '"'
                ? node.Display[1..^1]
                : node.Display;

        private static string DescribeException(Exception exception)
        {
            var sb = new StringBuilder(BevRedactor.RedactText(exception.Message));
            var inner = exception.InnerException;
            for (var hops = 0; inner is not null && hops < 5; hops++, inner = inner.InnerException)
            {
                sb.Append('\n').Append(inner.GetType().FullName).Append(": ").Append(BevRedactor.RedactText(inner.Message));
            }

            return Shorten(sb.ToString(), MaxMessageLength);
        }

        private static string ShortCategory(string category)
        {
            var dot = category.LastIndexOf('.');
            return dot < 0 ? category : category[(dot + 1)..];
        }

        private static string Shorten(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

        private static string? Safe(Func<string?> read)
        {
            try
            {
                return read();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return "«" + ex.GetType().Name + " while formatting»";
            }
        }
    }
}
