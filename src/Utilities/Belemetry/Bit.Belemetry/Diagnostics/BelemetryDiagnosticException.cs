namespace Bit.Belemetry;

/// <summary>
/// An exception as the diagnostic timeline keeps it: normalized and sanitized values only (type, message after the
/// privacy rules, a stack trace without file paths, the inner exceptions), never the exception object or its
/// <see cref="System.Exception.Data"/>.
/// </summary>
public sealed class BelemetryDiagnosticException
{
    /// <summary>The full type name (<c>System.InvalidOperationException</c>, <c>TypeError</c>).</summary>
    public string Type { get; init; } = "Exception";

    /// <summary>The message after the privacy rules; null when <see cref="BelemetryDiagnosticsOptions.IncludeExceptionMessages"/> is off.</summary>
    public string? Message { get; init; }

    /// <summary>The message with its values replaced (<c>Order {n} not found</c>): what grouping uses.</summary>
    public string? MessageTemplate { get; init; }

    /// <summary>The stack trace, without file paths and with its length bounded.</summary>
    public string? StackTrace { get; init; }

    /// <summary>The grouping fingerprint (<see cref="BelemetryExceptionFingerprint"/>), or the application's override.</summary>
    public string Fingerprint { get; init; } = string.Empty;

    /// <summary>The frame grouping anchored on: the first application frame, else the first frame.</summary>
    public string? Anchor { get; init; }

    /// <summary>
    /// Where it was observed: <c>unhandled</c> (renderer or circuit), <c>error-boundary</c>, <c>event-handler</c>,
    /// <c>js-interop</c>, <c>circuit</c>, <c>browser</c>, <c>tracked</c> (<see cref="IBelemetry.TrackException"/>), <c>log</c>.
    /// </summary>
    public string Origin { get; init; } = "log";

    /// <summary>Whether something handled it (an error boundary, the app's own catch) - null when not known.</summary>
    public bool? Handled { get; init; }

    /// <summary>The classification an <see cref="IBelemetryExceptionClassifier"/> gave it.</summary>
    public string? Classification { get; init; }

    /// <summary>Whether an <see cref="IBelemetryExceptionClassifier"/> marked it critical.</summary>
    public bool Critical { get; init; }

    /// <summary>The inner exceptions, outermost first (at most a few levels; an aggregate's first few).</summary>
    public IReadOnlyList<BelemetryDiagnosticException> InnerExceptions { get; init; } = [];

    /// <summary>Whether <see cref="Fingerprint"/> is an application override rather than the algorithm's.</summary>
    internal bool CustomFingerprint { get; init; }

    internal int EstimatedBytes
    {
        get
        {
            var size = 96 + ((Type.Length + (Message?.Length ?? 0) + (MessageTemplate?.Length ?? 0) + (StackTrace?.Length ?? 0) + Fingerprint.Length + (Anchor?.Length ?? 0)) * 2);
            foreach (var inner in InnerExceptions) size += inner.EstimatedBytes;
            return size;
        }
    }
}
