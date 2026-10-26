namespace Bit.BlazorUI;

/// <summary>
/// A transport- and provider-independent description of a failure, suitable for display and for
/// driving retry affordances.
/// </summary>
/// <remarks>
/// Transports translate their own exceptions into this shape so that UI components never branch on
/// a specific SDK exception type.
/// </remarks>
public sealed record BitAiError
{
    /// <summary>Creates an error.</summary>
    /// <param name="message">A human-readable message safe to show to the end user.</param>
    public BitAiError(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Message = message;
    }

    /// <summary>A human-readable message safe to show to the end user.</summary>
    public string Message { get; }

    /// <summary>A stable machine-readable code, for example <c>rate_limit</c>.</summary>
    public string? Code { get; init; }

    /// <summary>
    /// The kind of failure. A <see cref="BitAiErrorKind.Cancelled"/> one ends the message or the agent run it fails as
    /// cancelled rather than failed; the views draw every other kind alike, so the rest are the app's own to tell apart.
    /// </summary>
    public BitAiErrorKind Kind { get; init; } = BitAiErrorKind.Unknown;

    /// <summary>Whether retrying the same request could plausibly succeed.</summary>
    public bool IsRetryable { get; init; }

    /// <summary>How long to wait before retrying, when the provider says so.</summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>Additional detail, shown behind a disclosure rather than inline.</summary>
    public string? Detail { get; init; }

    /// <summary>The originating exception, if any. Never serialized.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates an error from an exception, mapping cancellation and timeouts onto their kinds.
    /// </summary>
    /// <param name="exception">The exception to translate.</param>
    /// <returns>The translated error.</returns>
    /// <remarks>
    /// Any other exception becomes a retryable <see cref="BitAiErrorKind.Unknown"/> error carrying its message, which is
    /// shown to the user - so a transport whose exceptions say what users must not read translates them itself. Its stack
    /// trace is never put in <see cref="Detail"/>: the detail is shown too, behind a disclosure, and a trace hands the user
    /// the internals of the server and its paths. The exception stays on <see cref="Exception"/>, for the app's logs.
    /// </remarks>
    public static BitAiError FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            OperationCanceledException => new BitAiError("The request was cancelled.")
            {
                Kind = BitAiErrorKind.Cancelled,
                Code = "cancelled",
                Exception = exception,
            },
            TimeoutException => new BitAiError("The request timed out.")
            {
                Kind = BitAiErrorKind.Timeout,
                Code = "timeout",
                IsRetryable = true,
                Exception = exception,
            },
            _ => new BitAiError(exception.Message)
            {
                Kind = BitAiErrorKind.Unknown,
                IsRetryable = true,
                Exception = exception,
            },
        };
    }
}

/// <summary>
/// Broad categories of failure, for an app's own recovery logic (<see cref="BitAiError.Kind"/>).
/// </summary>
public enum BitAiErrorKind
{
    /// <summary>Unclassified failure.</summary>
    Unknown = 0,

    /// <summary>The network or transport failed.</summary>
    Network = 1,

    /// <summary>Credentials are missing, invalid, or expired.</summary>
    Authentication = 2,

    /// <summary>The caller is authenticated but not permitted.</summary>
    Authorization = 3,

    /// <summary>The provider is rate limiting the caller.</summary>
    RateLimit = 4,

    /// <summary>The request exceeded the context window of the model.</summary>
    ContextLength = 5,

    /// <summary>A provider safety system refused the request.</summary>
    ContentFilter = 6,

    /// <summary>The provider rejected the request as malformed.</summary>
    InvalidRequest = 7,

    /// <summary>The provider failed internally.</summary>
    Provider = 8,

    /// <summary>A tool threw while executing.</summary>
    Tool = 9,

    /// <summary>The request timed out.</summary>
    Timeout = 10,

    /// <summary>The caller cancelled the request.</summary>
    Cancelled = 11,
}
