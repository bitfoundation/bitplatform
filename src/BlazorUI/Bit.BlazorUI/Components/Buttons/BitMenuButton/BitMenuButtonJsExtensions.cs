
namespace Bit.BlazorUI;

internal static class BitMenuButtonJsExtensions
{
    internal static async Task ToggleMenuButtonCallout(this IJSRuntime jsRuntime,
                                                           DotNetObjectReference<BitMenuButton> dotNetObjRef,
                                                           string uniqueId, 
                                                           string menuButtonId,
                                                           string menuButtonCalloutId, 
                                                           string menuButtonOverlayId,
                                                           bool isCalloutOpen)
    {
        await jsRuntime.InvokeVoidAsync("BitMenuButton.toggleMenuButtonCallout", dotNetObjRef, uniqueId, menuButtonId, menuButtonCalloutId, menuButtonOverlayId, isCalloutOpen);
    }
}
