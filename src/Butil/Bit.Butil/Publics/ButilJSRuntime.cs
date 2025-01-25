using System.Threading;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Butil;

/// <summary>
/// A wrapper around IJSRuntime that leverages <see cref="IJSInProcessRuntime"/> when available for enhanced JavaScript interop performance.
/// </summary>
public class ButilJSRuntime(IJSRuntime js) : IJSRuntime
{
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, default, args);
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (js is IJSInProcessRuntime jsInProcessRuntime)
            return ValueTask.FromResult(jsInProcessRuntime.Invoke<TValue>(identifier, args));

        return js.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }
}
