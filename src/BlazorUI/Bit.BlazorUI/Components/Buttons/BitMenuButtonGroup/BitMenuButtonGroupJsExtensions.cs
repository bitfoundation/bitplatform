
namespace Bit.BlazorUI;

internal static class BitMenuButtonGroupJsExtensions
{
    internal static async Task ToggleMenuButtonGroupCallout(this IJSRuntime jsRuntime,
                                                              DotNetObjectReference<BitMenuButtonGroup> dotNetObjRef,
                                                              string uniqueId, 
                                                              string menuButtonId,
                                                              string menuButtonCalloutId, 
                                                              string menuButtonOverlayId,
                                                              bool isCalloutOpen)
    {
        await jsRuntime.InvokeVoidAsync("BitMenuButtonGroup.toggleMenuButtonGroupCallout", dotNetObjRef, uniqueId, menuButtonId, menuButtonCalloutId, menuButtonOverlayId, isCalloutOpen);
    }
}
