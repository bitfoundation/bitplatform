using System.Diagnostics.CodeAnalysis;

namespace Microsoft.JSInterop;

internal static class BitCalloutsJsRuntimeExtension
{
    internal static async Task ToggleCallout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        string componentId,
        string calloutId,
        bool isCalloutOpen,
        DotNetObjectReference<T> dotNetObjRef) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitCallouts.toggleCallout", componentId, calloutId, isCalloutOpen, dotNetObjRef);
    }
    internal static async Task ClearCallout(this IJSRuntime jsRuntime, string calloutId)
    {
        await jsRuntime.InvokeVoidAsync("BitCallouts.clearCallout", calloutId);
    }
}
