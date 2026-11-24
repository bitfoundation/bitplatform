using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitMediaQueryJsRuntimeExtensions
{
    // Generic over the receiver of the reports (its JSInvokable "OnMatchChange"), so a component that only needs the
    // answer of a screen query - BitNavBar's drawer - listens through the same script without rendering a BitMediaQuery.
    internal static ValueTask BitMediaQuerySetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(this IJSRuntime jsRuntime,
                                                      string key,
                                                      string? elementId,
                                                      string? query,
                                                      string? screenQuery,
                                                      Dictionary<string, string>? breakpoints,
                                                      DotNetObjectReference<T>? dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.setup", key, elementId, query, screenQuery, breakpoints, dotnetObj);
    }

    // Answers synchronously on WebAssembly, where the call is made in process; see BitMediaQuery.MatchNow.
    internal static ValueTask<bool?> BitMediaQueryMatchNow(this IJSRuntime jsRuntime,
                                                               string? query,
                                                               string? screenQuery,
                                                               Dictionary<string, string>? breakpoints)
    {
        return jsRuntime.FastInvoke<bool?>("BitBlazorUI.MediaQuery.matchNow", query, screenQuery, breakpoints);
    }

    internal static ValueTask BitMediaQueryDispose(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.dispose", key);
    }
}
