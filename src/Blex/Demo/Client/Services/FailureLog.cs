namespace Bit.Blex.Demo.Client.Services;

/// <summary>One recorded action failure, flattened so the UI does not hold the live context.</summary>
/// <param name="Action">The qualified action name.</param>
/// <param name="Args">The arguments the action was called with, rendered.</param>
/// <param name="Message">The exception message.</param>
/// <param name="At">When it was observed.</param>
public sealed record ActionFailure(string Action, string Args, string Message, DateTimeOffset At);

/// <summary>
/// Sink for <see cref="BlexOptions.OnActionError"/>: the application's <em>own</em> action bodies
/// failing, as opposed to <see cref="ErrorLog"/>, which collects the non-fatal failures Blex
/// isolates from the pipeline around them. This is what a crash reporter subscribes to.
/// </summary>
public sealed class FailureLog
{
    private readonly List<ActionFailure> _failures = [];

    /// <summary>Raised after a failure is captured or the log is cleared.</summary>
    public event Action? Changed;

    /// <summary>The captured failures, newest first.</summary>
    public IReadOnlyList<ActionFailure> Failures => _failures;

    /// <summary>Captures an action failure. Wire this to <c>options.OnActionError</c>.</summary>
    public void Add(BlexActionErrorContext context)
    {
        var args = context.Args.Count == 0
            ? ""
            : string.Join(", ", context.Args.Select(a => $"{a.Name}: {a.Value}"));

        _failures.Insert(0, new ActionFailure(context.QualifiedName, args, context.Exception.Message, context.Timestamp));
        if (_failures.Count > 10)
            _failures.RemoveAt(_failures.Count - 1);

        Changed?.Invoke();
    }

    /// <summary>Empties the log.</summary>
    public void Clear()
    {
        _failures.Clear();
        Changed?.Invoke();
    }
}
