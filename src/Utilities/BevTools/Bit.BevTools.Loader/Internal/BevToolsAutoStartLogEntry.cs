using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// A log entry Bit.BevTools.Loader captured before BevTools was attached (<see cref="BevToolsAutoStart.Logs"/>). It is
/// what the entry was when it was logged, taken apart as Bit.BevTools' own logger provider takes an entry apart, but not
/// yet redacted or read at a value access: neither is known before a session exists. The attached session does both
/// when it takes the entry over, so it shows no more of it than of an entry logged after the attach. Nothing in it
/// references the objects the app logged.
/// </summary>
internal sealed class BevToolsAutoStartLogEntry
{
    public required DateTimeOffset At { get; init; }

    public required LogLevel Level { get; init; }

    public required string Category { get; init; }

    public int EventId { get; init; }

    public string? EventName { get; init; }

    /// <summary>
    /// False where the values were not read at all - no formatter, no <c>ToString</c>, no message - because the session
    /// that can attach there reads none (a process that serves many users, outside Development: value access None). The
    /// session then shows the entry as it would under None, whatever its own value access.
    /// </summary>
    public bool ValuesRead { get; init; } = true;

    /// <summary>The message as the logger formatted it, clipped; the session shows it only where it would show the logger's own text.</summary>
    public string? Formatted { get; init; }

    /// <summary>The message template ("{OriginalFormat}"), when the entry's state carries one.</summary>
    public string? Template { get; init; }

    /// <summary>The template's values, in the order the state lists them, at most as many as the provider reads.</summary>
    public IReadOnlyList<BevToolsAutoStartLogValue> Values { get; init; } = [];

    /// <summary>The state held more pairs than were read, so the formatted text holds values that were not.</summary>
    public bool ValuesTruncated { get; init; }

    public IReadOnlyList<BevToolsAutoStartLogScope> Scopes { get; init; } = [];

    public string? ExceptionType { get; init; }

    /// <summary>
    /// The exception's message, then each inner exception's type and message; unredacted until the session reads them.
    /// The messages are null where values are not read (<see cref="ValuesRead"/>).
    /// </summary>
    public IReadOnlyList<(string? Type, string? Message)>? ExceptionMessages { get; init; }

    public string? TraceId { get; init; }

    public string? SpanId { get; init; }
}
