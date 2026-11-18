namespace Bit.BevTools;

/// <summary>An error captured from any source (render, event handler, JS, HTTP, circuit, logger).</summary>
public sealed class BevToolsErrorRecord
{
    public required long Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required string Source { get; init; }

    public required string ExceptionType { get; init; }

    public required string Message { get; init; }

    /// <summary><see cref="Message"/> with its values replaced (<see cref="BevToolsErrorGroups.Template"/>): "Order {n} not found".</summary>
    public string MessageTemplate { get; init; } = "";

    /// <summary>The key of the errors that are one problem: source, type, message template, where it was thrown.</summary>
    public string Group { get; init; } = "";

    public string? StackTrace { get; init; }

    private string? _sourceLocation;

    /// <summary>
    /// Where it was thrown ("ErrorScenarios.razor:44"): from the stack trace's "in file:line", else from
    /// <see cref="ThrowSite"/> once the app's symbols are there.
    /// </summary>
    public string? SourceLocation
    {
        get => _sourceLocation ??= ThrowSite?.Resolve();
        init => _sourceLocation = value;
    }

    /// <summary>
    /// The first application frame of the exception, for when the stack trace's text names no file: a runtime with no
    /// symbols loaded (Blazor loads them into WebAssembly only in a browser it can debug) still knows each frame's
    /// method and IL offset, and BevTools reads the app's symbols itself - on WebAssembly they may arrive after the error.
    /// </summary>
    internal BevToolsThrowSite? ThrowSite { get; init; }

    public long? ComponentInstanceId { get; init; }

    public string? ComponentName { get; init; }

    /// <summary>The UI event or activity that immediately preceded the error, when known.</summary>
    public long? PrecedingEventId { get; init; }

    public long TimelineEventId { get; set; }

    /// <summary>Stable key for grouping repeats.</summary>
    public required string Fingerprint { get; init; }

    public int Count { get; internal set; } = 1;

    public DateTimeOffset LastSeen { get; internal set; }

    /// <summary>
    /// "Type: message" of the innermost exception when it differs from the outer one. The exception object itself is
    /// deliberately not retained: its graph can reference components, services and closures, and the error center
    /// keeps hundreds of records for the lifetime of the session.
    /// </summary>
    public string? InnerError { get; init; }
}
