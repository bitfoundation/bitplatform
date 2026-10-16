using System.Diagnostics;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Bit.BevTools;

/// <summary>
/// Decorates an <see cref="IJSRuntime"/> to record .NET → JS calls with duration, result and originating component.
/// Applied automatically to <c>[Inject] IJSRuntime</c> properties of tracked components; services can opt in with
/// <see cref="BevToolsJSRuntimeExtensions.WithBevToolsTracking"/>.
/// </summary>
public class BevTrackingJSRuntime : IJSRuntime
{
    internal BevTrackingJSRuntime(IJSRuntime inner, BevToolsSession session, BevComponentRecord? component)
    {
        Inner = inner;
        Session = session;
        Component = component;
    }

    public IJSRuntime Inner { get; }

    internal BevToolsSession Session { get; }

    internal BevComponentRecord? Component { get; }

    internal static BevTrackingJSRuntime Create(IJSRuntime inner, BevToolsSession session, BevComponentRecord? component)
    {
        while (inner is BevTrackingJSRuntime tracking)
        {
            inner = tracking.Inner;
        }

        return inner is IJSInProcessRuntime inProcess
            ? new BevTrackingInProcessJSRuntime(inProcess, session, component)
            : new BevTrackingJSRuntime(inner, session, component);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args = Unwrap(args);
        var record = Begin<TValue>(identifier, args, isModule: false, isSync: false);
        try
        {
            var result = await Inner.InvokeAsync<TValue>(identifier, cancellationToken, args).ConfigureAwait(false);
            End(record, null);
            return Wrap(result);
        }
        catch (Exception ex)
        {
            End(record, ex);
            throw;
        }
    }

    /// <summary>
    /// Replaces tracking wrappers in an argument array with the framework's own references.
    /// The JSON converter that turns an <see cref="IJSObjectReference"/> into <c>{"__jsObjectId":n}</c> only matches
    /// the framework's concrete type; a wrapper would silently be serialized as a plain object and the JS side would
    /// receive garbage. Only top-level arguments can be repaired, which is why wrapping is optional
    /// (<see cref="BitBevToolsOptions.TrackJsModuleReferences"/>).
    /// </summary>
    internal static object?[]? Unwrap(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        object?[]? copy = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is BevTrackingJSObjectReference wrapper)
            {
                copy ??= (object?[])args.Clone();
                copy[i] = wrapper.Inner;
            }
        }

        return copy ?? args;
    }

    internal TValue Wrap<TValue>(TValue result)
    {
        if (!Session.Options.TrackJsModuleReferences)
        {
            return result;
        }

        if (result is IJSObjectReference reference && result is not BevTrackingJSObjectReference)
        {
            object wrapped = reference is IJSInProcessObjectReference inProcess
                ? new BevTrackingInProcessJSObjectReference(inProcess, this)
                : new BevTrackingJSObjectReference(reference, this);
            if (wrapped is TValue typed)
            {
                return typed;
            }
        }

        return result;
    }

    /// <summary>Starts a record, or returns null while capture is paused (the call itself still goes through).</summary>
    internal BevJsInteropRecord? Begin<TValue>(string identifier, object?[]? args, bool isModule, bool isSync)
    {
        if (!Session.IsCapturing)
        {
            return null;
        }

        var record = new BevJsInteropRecord
        {
            Id = Session.Interop.NextId(),
            At = DateTimeOffset.UtcNow,
            StartTicks = Stopwatch.GetTimestamp(),
            Direction = BevJsInteropDirection.DotNetToJs,
            Identifier = identifier,
            ArgumentCount = args?.Length ?? 0,
            ArgumentTypes = args is { Length: > 0 } ? string.Join(", ", args.Select(a => a is null ? "null" : TypeNames.Short(a.GetType()))) : null,
            ResultType = typeof(TValue) == typeof(IJSVoidResult) ? "void" : TypeNames.Short(typeof(TValue)),
            ComponentInstanceId = Component?.InstanceId,
            ComponentName = Component?.DisplayName,
            IsModuleCall = isModule,
            IsSync = isSync,
        };
        record.TimelineEventId = Session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.JsInterop,
            Category = "js-interop",
            Title = "→ JS " + identifier,
            Detail = (isSync ? "sync" : "async") + (record.ArgumentTypes is null ? ", no arguments" : ", args: " + record.ArgumentTypes) + ", returns " + record.ResultType,
            ComponentInstanceId = Component?.InstanceId,
            ComponentName = Component?.DisplayName,
            ParentEventId = ActivityObserver.CurrentTriggerEvent(Session)?.Id,
            Data = new Dictionary<string, object?> { ["callId"] = record.Id },
        });
        Session.Interop.Add(record);
        return record;
    }

    /// <summary>Completes the record, if there is one; a failure is always sent to the error list, paused or not.</summary>
    internal void End(BevJsInteropRecord? record, Exception? exception)
    {
        if (record is not null)
        {
            var elapsed = Stopwatch.GetElapsedTime(record.StartTicks).TotalMilliseconds;
            var error = exception is null ? null : exception.GetType().Name + ": " + exception.Message;
            Session.Interop.Complete(record, elapsed, exception is null, error);
            var severity = exception is not null ? BevToolsSeverity.Error : elapsed >= Session.Options.Diagnostics.SlowJsInteropMs ? BevToolsSeverity.Warning : BevToolsSeverity.Info;
            Session.Timeline.Complete(record.TimelineEventId, elapsed, exception is null ? null : "failed: " + error, severity);
        }

        if (exception is not null and not OperationCanceledException)
        {
            Session.Errors.Record(exception, "js-interop", component: Component);
        }
    }
}

public sealed class BevTrackingInProcessJSRuntime : BevTrackingJSRuntime, IJSInProcessRuntime
{
    private readonly IJSInProcessRuntime _inner;

    internal BevTrackingInProcessJSRuntime(IJSInProcessRuntime inner, BevToolsSession session, BevComponentRecord? component) : base(inner, session, component)
    {
        _inner = inner;
    }

    public TResult Invoke<TResult>(string identifier, params object?[]? args)
    {
        args = Unwrap(args);
        var record = Begin<TResult>(identifier, args, isModule: false, isSync: true);
        try
        {
            var result = _inner.Invoke<TResult>(identifier, args);
            End(record, null);
            return Wrap(result);
        }
        catch (Exception ex)
        {
            End(record, ex);
            throw;
        }
    }
}

public class BevTrackingJSObjectReference : IJSObjectReference
{
    internal BevTrackingJSObjectReference(IJSObjectReference inner, BevTrackingJSRuntime runtime)
    {
        Inner = inner;
        Runtime = runtime;
    }

    public IJSObjectReference Inner { get; }

    internal BevTrackingJSRuntime Runtime { get; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args = BevTrackingJSRuntime.Unwrap(args);
        var record = Runtime.Begin<TValue>("module." + identifier, args, isModule: true, isSync: false);
        try
        {
            var result = await Inner.InvokeAsync<TValue>(identifier, cancellationToken, args).ConfigureAwait(false);
            Runtime.End(record, null);
            return Runtime.Wrap(result);
        }
        catch (Exception ex)
        {
            Runtime.End(record, ex);
            throw;
        }
    }

    public ValueTask DisposeAsync() => Inner.DisposeAsync();
}

public sealed class BevTrackingInProcessJSObjectReference : BevTrackingJSObjectReference, IJSInProcessObjectReference
{
    private readonly IJSInProcessObjectReference _inner;

    internal BevTrackingInProcessJSObjectReference(IJSInProcessObjectReference inner, BevTrackingJSRuntime runtime) : base(inner, runtime)
    {
        _inner = inner;
    }

    public TValue Invoke<TValue>(string identifier, params object?[]? args)
    {
        args = BevTrackingJSRuntime.Unwrap(args);
        var record = Runtime.Begin<TValue>("module." + identifier, args, isModule: true, isSync: true);
        try
        {
            var result = _inner.Invoke<TValue>(identifier, args);
            Runtime.End(record, null);
            return Runtime.Wrap(result);
        }
        catch (Exception ex)
        {
            Runtime.End(record, ex);
            throw;
        }
    }

    public void Dispose() => _inner.Dispose();
}

public static class BevToolsJSRuntimeExtensions
{
    /// <summary>
    /// Returns a runtime that records calls in the BevTools timeline. Use in services that make JS calls; components
    /// are wrapped automatically. Returns the original runtime when BevTools is disabled.
    /// </summary>
    public static IJSRuntime WithBevToolsTracking(this IJSRuntime jsRuntime, BevToolsSession session)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(session);
        return session.IsEnabled ? BevTrackingJSRuntime.Create(jsRuntime, session, null) : jsRuntime;
    }
}
