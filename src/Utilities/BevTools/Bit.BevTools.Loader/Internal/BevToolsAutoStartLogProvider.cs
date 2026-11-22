using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// The logger provider <see cref="BevToolsAutoStart.Logs"/> registers at startup, so it is in the app's logger factory
/// from its first entry, while BevTools itself waits to be attached. It keeps what it captures in
/// <see cref="BevToolsAutoStartLogs"/> and nothing else: no session, no timeline, no component. Its alias is Bit.BevTools'
/// own, so <c>Logging:BevTools:LogLevel</c> filters it exactly as it filters the session's provider after the attach.
/// <para>
/// It reads what Bit.BevTools' provider reads, the same way and within the same bounds, but redacts nothing and reads
/// at no value access yet: neither exists before a session does. The values it keeps are scalars and texts, never the
/// app's objects (<see cref="BevToolsAutoStartLogValue"/>), and the session applies both when it takes them over.
/// Being in every startup download, it uses no reflection, and swallows every failure: the factory would otherwise throw
/// it to the code that logged.
/// </para>
/// </summary>
[ProviderAlias("BevTools")]
internal sealed class BevToolsAutoStartLogProvider(BevToolsAutoStartLogs logs) : ILoggerProvider, ISupportExternalScope
{
    // The bounds of Bit.BevTools' own provider: what it would have read of an entry, and no more.
    internal const int MaxPairs = 128;
    internal const int MaxScopes = 8;
    internal const int MaxScopePairs = 20;
    // Twice what the session shows, which it cuts to after redacting: cut before, a credential at the end could lose
    // the part that makes it recognisable and stay readable.
    internal const int MaxMessageLength = 8000;
    internal const int MaxScopeLength = 600;
    private const int MaxInnerExceptions = 5;

    // A value's ToString, a scope's or a formatter can log in turn; what the capture itself causes is not the app's.
    [ThreadStatic]
    private static bool _capturing;

    private IExternalScopeProvider? _scopes;

    internal BevToolsAutoStartLogs Logs { get; } = logs;

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public ILogger CreateLogger(string categoryName) =>
        // BevTools' own category, which its session provider does not capture either: the attach's audit line included.
        categoryName == BevToolsLoaderContext.AssemblyName
            ? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance
            : new CaptureLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class CaptureLogger(BevToolsAutoStartLogProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => provider._scopes?.Push(state);

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None && logLevel >= provider.Logs.Context.Options.MinimumLogLevel && provider.Logs.IsCapturing;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_capturing || !IsEnabled(logLevel) || BevToolsHooks.IsOwnWork || provider.Logs.Current() is not { } buffer)
            {
                return;
            }

            _capturing = true;
            try
            {
                buffer.Add(Capture(logLevel, eventId, state, exception, formatter));
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

        private BevToolsAutoStartLogEntry Capture<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Read once per entry: where the session that can attach reads no values, none of the app's code runs for them.
            var read = provider.Logs.ReadsValues;
            string? template = null;
            List<BevToolsAutoStartLogValue>? values = null;
            var truncated = false;
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

                    (values ??= []).Add(read ? BevToolsAutoStartLogValue.Capture(name, value) : BevToolsAutoStartLogValue.Unread(name, value));
                }

                if (pairs.Count > count)
                {
                    truncated = true;
                    if (pairs[^1] is { Key: "{OriginalFormat}", Value: string last })
                    {
                        template = last;
                    }
                }
            }

            var activity = Activity.Current;
            var w3c = activity is { IdFormat: ActivityIdFormat.W3C };
            return new BevToolsAutoStartLogEntry
            {
                At = DateTimeOffset.UtcNow,
                Level = logLevel,
                Category = category,
                EventId = eventId.Id,
                EventName = eventId.Name,
                ValuesRead = read,
                Formatted = read ? Clip(Safe(() => formatter(state, exception)), MaxMessageLength) : null,
                Template = template is null ? null : Clip(template, MaxMessageLength),
                Values = values ?? [],
                ValuesTruncated = truncated,
                Scopes = ReadScopes(read),
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessages = exception is null ? null : ReadException(exception),
                TraceId = w3c ? activity!.TraceId.ToHexString() : null,
                SpanId = w3c ? activity!.SpanId.ToHexString() : null,
            };
        }

        private List<BevToolsAutoStartLogScope> ReadScopes(bool read)
        {
            List<BevToolsAutoStartLogScope> scopes = [];
            provider._scopes?.ForEachScope(static (scope, state) =>
            {
                var (scopes, read) = state;
                if (scope is null || scopes.Count >= MaxScopes)
                {
                    return;
                }

                if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
                {
                    List<BevToolsAutoStartLogValue> values = [];
                    foreach (var (key, value) in pairs.Take(MaxScopePairs))
                    {
                        values.Add(read ? BevToolsAutoStartLogValue.Capture(key, value) : BevToolsAutoStartLogValue.Unread(key, value));
                    }

                    scopes.Add(new BevToolsAutoStartLogScope { Pairs = values, Type = scope.GetType() });
                    return;
                }

                scopes.Add(new BevToolsAutoStartLogScope
                {
                    Text = !read ? null : Clip(scope as string ?? BevToolsAutoStartLogValue.SafeToString(scope), MaxScopeLength),
                    Type = scope.GetType(),
                });
            }, (scopes, read));
            return scopes;
        }

        private static List<(string? Type, string Message)> ReadException(Exception exception)
        {
            List<(string? Type, string Message)> messages = [(null, exception.Message)];
            var inner = exception.InnerException;
            for (var hops = 0; inner is not null && hops < MaxInnerExceptions; hops++, inner = inner.InnerException)
            {
                messages.Add((inner.GetType().FullName, inner.Message));
            }

            return messages;
        }

        private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

        private static string Safe(Func<string?> read)
        {
            try
            {
                return read() ?? "";
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return "«" + ex.GetType().Name + " while formatting»";
            }
        }
    }
}
