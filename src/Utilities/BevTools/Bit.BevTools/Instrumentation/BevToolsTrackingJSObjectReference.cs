using Microsoft.JSInterop;

namespace Bit.BevTools;

public class BevToolsTrackingJSObjectReference : IJSObjectReference
{
    internal BevToolsTrackingJSObjectReference(IJSObjectReference inner, BevToolsTrackingJSRuntime runtime)
    {
        Inner = inner;
        Runtime = runtime;
    }

    public IJSObjectReference Inner { get; }

    internal BevToolsTrackingJSRuntime Runtime { get; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args = BevToolsTrackingJSRuntime.Unwrap(args);
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

    public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args) => InvokeConstructorAsync(identifier, CancellationToken.None, args);

    public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args = BevToolsTrackingJSRuntime.Unwrap(args);
        return Runtime.TrackAsync("new module." + identifier, args, isModule: true, () => Inner.InvokeConstructorAsync(identifier, cancellationToken, args));
    }

    public ValueTask<TValue> GetValueAsync<TValue>(string identifier) => GetValueAsync<TValue>(identifier, CancellationToken.None);

    public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken) =>
        Runtime.TrackAsync("get module." + identifier, null, isModule: true, () => Inner.GetValueAsync<TValue>(identifier, cancellationToken));

    public ValueTask SetValueAsync<TValue>(string identifier, TValue value) => SetValueAsync(identifier, value, CancellationToken.None);

    public ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken)
    {
        value = BevToolsTrackingJSRuntime.UnwrapValue(value);
        return Runtime.TrackVoidAsync("set module." + identifier, [value], isModule: true, () => Inner.SetValueAsync(identifier, value, cancellationToken));
    }

    public ValueTask DisposeAsync() => Inner.DisposeAsync();
}
