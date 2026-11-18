using Microsoft.JSInterop;

namespace Bit.BevTools;

public sealed class BevToolsTrackingInProcessJSRuntime : BevToolsTrackingJSRuntime, IJSInProcessRuntime
{
    private readonly IJSInProcessRuntime _inner;

    internal BevToolsTrackingInProcessJSRuntime(IJSInProcessRuntime inner, BevToolsSession session, BevToolsComponentRecord? component) : base(inner, session, component)
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

    public IJSInProcessObjectReference InvokeConstructor(string identifier, params object?[]? args)
    {
        args = Unwrap(args);
        return Track("new " + identifier, args, isModule: false, () => _inner.InvokeConstructor(identifier, args));
    }

    public TValue GetValue<TValue>(string identifier) => Track("get " + identifier, null, isModule: false, () => _inner.GetValue<TValue>(identifier));

    public void SetValue<TValue>(string identifier, TValue value)
    {
        value = UnwrapValue(value);
        TrackVoid("set " + identifier, [value], isModule: false, () => _inner.SetValue(identifier, value));
    }
}
