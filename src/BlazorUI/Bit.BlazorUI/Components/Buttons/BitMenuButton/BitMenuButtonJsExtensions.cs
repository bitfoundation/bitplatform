
namespace Bit.BlazorUI;

internal static class BitMenuButtonJsExtensions
{
    internal static async Task ToggleMenuButtonCallout<T>(this IJSRuntime jsRuntime,
                                                              DotNetObjectReference<BitMenuButton<T>> dotNetObjRef,
                                                              string uniqueId,
                                                              string calloutId,
                                                              string overlayId,
                                                              bool isCalloutOpen) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitMenuButton.toggleMenuButtonCallout", dotNetObjRef, uniqueId, calloutId, overlayId, isCalloutOpen);
    }
}
