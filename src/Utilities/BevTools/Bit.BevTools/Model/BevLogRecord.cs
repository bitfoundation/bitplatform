using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// One entry an <c>ILogger</c> wrote while this session's code ran: its level, category, event id, message, the
/// structured values of its message template, its scopes and where it came from. Everything is captured as text when
/// it is logged, redacted and read at the session's <see cref="BevValueAccess"/>; no object the application logged is
/// kept.
/// </summary>
public sealed class BevLogRecord
{
    public required long Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required LogLevel Level { get; init; }

    /// <summary>The logger's category, usually the full name of the type that logs: "MyApp.Services.OrderService".</summary>
    public required string Category { get; init; }

    public int EventId { get; init; }

    public string? EventName { get; init; }

    /// <summary>
    /// The message as the logger formatted it, redacted; null when <see cref="BevValueAccess.None"/> keeps the values it
    /// was formatted from unread (<see cref="MessageTemplate"/> still says what was logged, when it has holes).
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// The message template of a structured entry ("Loaded {Count} orders in {ElapsedMs} ms"), when it has one. Under
    /// <see cref="BevValueAccess.None"/> only one with holes: a message without any may be an interpolated string, which
    /// is its own template, values and all.
    /// </summary>
    public string? MessageTemplate { get; init; }

    /// <summary>The template's values by name, each as the inspector shows it at the session's value access, redacted.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Properties { get; init; } = [];

    /// <summary>The scopes the entry was written in (<c>ILogger.BeginScope</c>), outermost first, each as one line.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    public string? ExceptionType { get; init; }

    /// <summary>The exception's message, redacted; its inner exceptions' types and messages follow on their own lines.</summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>The W3C trace id of the activity current when it was logged: what a tracing backend files it under.</summary>
    public string? TraceId { get; init; }

    public string? SpanId { get; init; }

    /// <summary>The UI event or navigation running when it was logged, when one was.</summary>
    public long? TriggerEventId { get; init; }

    /// <summary>The component method that logged it ("OrderRow.OnInitializedAsync"), when the stack shows one.</summary>
    public string? CallerComponent { get; init; }

    /// <summary>The application method the component logged through ("OrderService.LoadAsync"), when there is one.</summary>
    public string? CallerVia { get; init; }

    /// <summary>"OrderRow.OnInitializedAsync via OrderService.LoadAsync", or null when no component is on the stack.</summary>
    public string? Caller => CallerComponent is null ? null : CallerVia is null ? CallerComponent : CallerComponent + " via " + CallerVia;

    /// <summary>The database command the entry reports, when it is one of Entity Framework Core's (<see cref="BevDbCommands"/>).</summary>
    public BevDbCommand? DbCommand { get; init; }

    /// <summary>The error the entry was also recorded as (an error or critical entry, a Blazor warning), when it was one.</summary>
    public long? ErrorId { get; internal set; }

    public long TimelineEventId { get; internal set; }

    /// <summary>What a list shows for it: the message, else its template, else that neither may be shown.</summary>
    public string Text => Message ?? MessageTemplate ?? BevRedactor.HiddenValue;

    /// <summary>"Information" as the short label consoles use: "info", "warn", "fail", "crit", "dbug", "trce".</summary>
    public string LevelLabel => BevLogLevels.Label(Level);

    /// <summary>The category without its namespace: "OrderService".</summary>
    public string ShortCategory
    {
        get
        {
            var generic = Category.IndexOf('[');
            var dot = (generic < 0 ? Category : Category[..generic]).LastIndexOf('.');
            return dot < 0 ? Category : Category[(dot + 1)..];
        }
    }
}

/// <summary>Log levels as BevTools names and parses them.</summary>
public static class BevLogLevels
{
    public static string Label(LogLevel level) => level switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => "none",
    };

    /// <summary>Reads "warning", "warn", "Error", "fail", "info", "4"… (what a filter's <c>level:</c> accepts); null for anything else.</summary>
    public static LogLevel? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();
        if (int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number))
        {
            return number is >= 0 and <= 5 ? (LogLevel)number : null;
        }

        foreach (var level in Enum.GetValues<LogLevel>())
        {
            if (level != LogLevel.None && (Label(level).Equals(text, StringComparison.OrdinalIgnoreCase) || level.ToString().StartsWith(text, StringComparison.OrdinalIgnoreCase)))
            {
                return level;
            }
        }

        return null;
    }
}
