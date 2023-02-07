
namespace Bit.BlazorUI;

internal static class BitMenuButtonListJsExtensions
{
    internal static async Task ToggleMenuButtonListCallout<T>(this IJSRuntime jsRuntime,
                                                              DotNetObjectReference<BitMenuButtonList<T>> dotNetObjRef,
                                                              string uniqueId, 
                                                              string menuButtonId,
                                                              string menuButtonCalloutId, 
                                                              string menuButtonOverlayId,
                                                              bool isCalloutOpen)
    {
        await jsRuntime.InvokeVoidAsync("BitMenuButtonList.toggleMenuButtonListCallout", dotNetObjRef, uniqueId, menuButtonId, menuButtonCalloutId, menuButtonOverlayId, isCalloutOpen);
    }
}
