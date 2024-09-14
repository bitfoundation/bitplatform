using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    public static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (IsInvalidRuntime(jsRuntime)) return;

        await jsRuntime.InvokeVoidAsync(identifier, args);
    }

    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (IsInvalidRuntime(jsRuntime)) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, args);
    }

    private static bool IsInvalidRuntime(IJSRuntime jsRuntime)
    {
        var type = jsRuntime.GetType();

        if (type.Name is not "RemoteJSRuntime" and "UnsupportedJavaScriptRuntime") return false; // Blazor WASM/Hybrid

        return (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)! is false;
    }
}
