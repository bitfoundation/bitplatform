using System.Diagnostics;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Bit.BevTools;

/// <summary>
/// Decorates an <see cref="IJSRuntime"/> to record .NET → JS calls with duration, result and originating component.
/// Applied automatically to <c>[Inject] IJSRuntime</c> properties of tracked components; services can opt in with
/// <see cref="BevToolsJSRuntimeExtensions.WithBevToolsTracking"/>.
/// </summary>
public class BevToolsTrackingJSRuntime : IJSRuntime
{
    internal BevToolsTrackingJSRuntime(IJSRuntime inner, BevToolsSession session, BevToolsComponentRecord? component)
    {
        Inner = inner;
        Session = session;
        Component = component;
    }

    public IJSRuntime Inner { get; }

    internal BevToolsSession Session { get; }

    internal BevToolsComponentRecord? Component { get; }

    internal static BevToolsTrackingJSRuntime Create(IJSRuntime inner, BevToolsSession session, BevToolsComponentRecord? component)
    {
        while (inner is BevToolsTrackingJSRuntime tracking)
        {
            inner = tracking.Inner;
        }

        return inner is IJSInProcessRuntime inProcess
            ? new BevToolsTrackingInProcessJSRuntime(inProcess, session, component)
            : new BevToolsTrackingJSRuntime(inner, session, component);
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

    // .NET 10 added these to IJSRuntime as default interface members that throw NotImplementedException. A wrapper
    // that left them to the defaults would break working application code the moment BevTools patched it in.
    public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args) => InvokeConstructorAsync(identifier, CancellationToken.None, args);

    public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args = Unwrap(args);
        return TrackAsync("new " + identifier, args, isModule: false, () => Inner.InvokeConstructorAsync(identifier, cancellationToken, args));
    }

    public ValueTask<TValue> GetValueAsync<TValue>(string identifier) => GetValueAsync<TValue>(identifier, CancellationToken.None);

    public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken) =>
        TrackAsync("get " + identifier, null, isModule: false, () => Inner.GetValueAsync<TValue>(identifier, cancellationToken));

    public ValueTask SetValueAsync<TValue>(string identifier, TValue value) => SetValueAsync(identifier, value, CancellationToken.None);

    public ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken)
    {
        value = UnwrapValue(value);
        return TrackVoidAsync("set " + identifier, [value], isModule: false, () => Inner.SetValueAsync(identifier, value, cancellationToken));
    }

    /// <summary>Records one asynchronous call made through <paramref name="call"/>, wrapping a returned object reference.</summary>
    internal async ValueTask<TValue> TrackAsync<TValue>(string identifier, object?[]? args, bool isModule, Func<ValueTask<TValue>> call)
    {
        var record = Begin<TValue>(identifier, args, isModule, isSync: false);
        try
        {
            var result = await call().ConfigureAwait(false);
            End(record, null);
            return Wrap(result);
        }
        catch (Exception ex)
        {
            End(record, ex);
            throw;
        }
    }

    internal async ValueTask TrackVoidAsync(string identifier, object?[]? args, bool isModule, Func<ValueTask> call)
    {
        var record = Begin<IJSVoidResult>(identifier, args, isModule, isSync: false);
        try
        {
            await call().ConfigureAwait(false);
            End(record, null);
        }
        catch (Exception ex)
        {
            End(record, ex);
            throw;
        }
    }

    internal TValue Track<TValue>(string identifier, object?[]? args, bool isModule, Func<TValue> call)
    {
        var record = Begin<TValue>(identifier, args, isModule, isSync: true);
        try
        {
            var result = call();
            End(record, null);
            return Wrap(result);
        }
        catch (Exception ex)
        {
            End(record, ex);
            throw;
        }
    }

    internal void TrackVoid(string identifier, object?[]? args, bool isModule, Action call)
    {
        var record = Begin<IJSVoidResult>(identifier, args, isModule, isSync: true);
        try
        {
            call();
            End(record, null);
        }
        catch (Exception ex)
        {
            End(record, ex);
            throw;
        }
    }

    /// <summary>The single-value form of <see cref="Unwrap"/>, for a value passed as a typed argument.</summary>
    internal static TValue UnwrapValue<TValue>(TValue value) =>
        value is BevToolsTrackingJSObjectReference wrapper && wrapper.Inner is TValue inner ? inner : value;

    /// <summary>
    /// Replaces tracking wrappers in an argument array with the framework's own references.
    /// The JSON converter that turns an <see cref="IJSObjectReference"/> into <c>{"__jsObjectId":n}</c> only matches
    /// the framework's concrete type; a wrapper would silently be serialized as a plain object and the JS side would
    /// receive garbage. Only top-level arguments can be repaired, which is why wrapping is optional
    /// (<see cref="BevToolsOptions.TrackJsModuleReferences"/>).
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
            if (args[i] is BevToolsTrackingJSObjectReference wrapper)
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

        if (result is IJSObjectReference reference && result is not BevToolsTrackingJSObjectReference)
        {
            object wrapped = reference is IJSInProcessObjectReference inProcess
                ? new BevToolsTrackingInProcessJSObjectReference(inProcess, this)
                : new BevToolsTrackingJSObjectReference(reference, this);
            if (wrapped is TValue typed)
            {
                return typed;
            }
        }

        return result;
    }

    /// <summary>Starts a record, or returns null while capture is paused (the call itself still goes through).</summary>
    internal BevToolsJsInteropRecord? Begin<TValue>(string identifier, object?[]? args, bool isModule, bool isSync)
    {
        if (!Session.IsCapturing)
        {
            return null;
        }

        var record = new BevToolsJsInteropRecord
        {
            Id = Session.Interop.NextId(),
            At = DateTimeOffset.UtcNow,
            StartTicks = Stopwatch.GetTimestamp(),
            Direction = BevToolsJsInteropDirection.DotNetToJs,
            Identifier = identifier,
            ArgumentCount = args?.Length ?? 0,
            ArgumentTypes = args is { Length: > 0 } ? string.Join(", ", args.Select(a => a is null ? "null" : BevToolsTypeNames.Short(a.GetType()))) : null,
            ResultType = typeof(TValue) == typeof(IJSVoidResult) ? "void" : BevToolsTypeNames.Short(typeof(TValue)),
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
            ParentEventId = BevToolsActivityObserver.CurrentTriggerEvent(Session)?.Id,
            Data = new Dictionary<string, object?> { ["callId"] = record.Id },
        });
        Session.Interop.Add(record);
        return record;
    }

    /// <summary>Completes the record, if there is one; a failure is always sent to the error list, paused or not.</summary>
    internal void End(BevToolsJsInteropRecord? record, Exception? exception)
    {
        if (record is not null)
        {
            var elapsed = Stopwatch.GetElapsedTime(record.StartTicks).TotalMilliseconds;
            var error = exception is null ? null : exception.GetType().Name + ": " + BevToolsRedactor.RedactText(exception.Message);
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
