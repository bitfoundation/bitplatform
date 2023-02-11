
namespace Bit.BlazorUI;

internal static class BitMenuButtonJsExtensions
{
    internal static async Task ToggleMenuButtonCallout<T>(this IJSRuntime jsRuntime,
                                                              DotNetObjectReference<BitMenuButton<T>> dotNetObjRef,
                                                              string uniqueId,
                                                              string menuButtonId,
                                                              string menuButtonCalloutId,
                                                              string menuButtonOverlayId,
                                                              bool isCalloutOpen) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitMenuButton.toggleMenuButtonCallout", dotNetObjRef, uniqueId, menuButtonId, menuButtonCalloutId, menuButtonOverlayId, isCalloutOpen);
    }
}
