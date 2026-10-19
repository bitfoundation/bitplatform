namespace Bit.Blex;

/// <summary>
/// Context describing an action whose body threw. Delivered to
/// <see cref="IBlexMiddleware.OnActionError"/> and <see cref="BlexManager.ActionFailed"/> -- the
/// counterpart to Pinia's <c>$onAction</c> <c>onError</c> hook.
/// </summary>
/// <remarks>
/// The exception still propagates after the hooks run: an <c>[BlexEffectAttribute]</c> wrapper
/// captures it into its <c>XxxError</c> property, and a plain <c>[BlexActionAttribute]</c> lets it
/// reach the caller. This is an observation point (logging, telemetry, crash reporting), not a
/// handler -- nothing here can swallow the failure. Cancellation is not reported: an
/// <see cref="OperationCanceledException"/> is the documented outcome of <c>CancelXxx()</c>,
/// <see cref="BlexEffectConcurrency.Latest"/> supersession and store disposal.
/// </remarks>
public sealed class BlexActionErrorContext
{
    internal BlexActionErrorContext(IBlexStore store, string actionName, IReadOnlyList<BlexActionArg>? args, Exception exception)
    {
        Store = store;
        ActionName = actionName;
        Args = args ?? [];
        Exception = exception;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>The store whose action failed.</summary>
    public IBlexStore Store { get; }

    /// <summary>The action name (e.g. <c>"LoadUser"</c>).</summary>
    public string ActionName { get; }

    /// <summary>The action's arguments, so a report can name the input that failed.</summary>
    public IReadOnlyList<BlexActionArg> Args { get; }

    /// <summary>The exception the action body threw.</summary>
    public Exception Exception { get; }

    /// <summary>When the failure was observed (UTC).</summary>
    public DateTimeOffset Timestamp { get; }

    private string? _qualifiedName;

    /// <summary>Fully-qualified action label including the originating store.</summary>
    public string QualifiedName => _qualifiedName ??= $"{Store.Name}/{ActionName}";
}
