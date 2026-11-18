using Microsoft.JSInterop;

namespace Bit.BevTools;

public sealed class BevToolsTrackingInProcessJSObjectReference : BevToolsTrackingJSObjectReference, IJSInProcessObjectReference
{
    private readonly IJSInProcessObjectReference _inner;

    internal BevToolsTrackingInProcessJSObjectReference(IJSInProcessObjectReference inner, BevToolsTrackingJSRuntime runtime) : base(inner, runtime)
    {
        _inner = inner;
    }

    public TValue Invoke<TValue>(string identifier, params object?[]? args)
    {
        args = BevToolsTrackingJSRuntime.Unwrap(args);
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

    public IJSInProcessObjectReference InvokeConstructor(string identifier, params object?[]? args)
    {
        args = BevToolsTrackingJSRuntime.Unwrap(args);
        return Runtime.Track("new module." + identifier, args, isModule: true, () => _inner.InvokeConstructor(identifier, args));
    }

    public TValue GetValue<TValue>(string identifier) => Runtime.Track("get module." + identifier, null, isModule: true, () => _inner.GetValue<TValue>(identifier));

    public void SetValue<TValue>(string identifier, TValue value)
    {
        value = BevToolsTrackingJSRuntime.UnwrapValue(value);
        Runtime.TrackVoid("set module." + identifier, [value], isModule: true, () => _inner.SetValue(identifier, value));
    }

    public void Dispose() => _inner.Dispose();
}
