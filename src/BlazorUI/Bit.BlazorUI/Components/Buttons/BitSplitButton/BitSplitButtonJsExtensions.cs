
namespace Bit.BlazorUI;

internal static class BitSplitButtonJsExtensions
{
    internal static async Task ToggleSplitButtonCallout<T>(this IJSRuntime jsRuntime,
                                                              DotNetObjectReference<BitSplitButton<T>> dotNetObjRef,
                                                              string uniqueId, 
                                                              string menuButtonId,
                                                              string menuButtonCalloutId, 
                                                              string menuButtonOverlayId,
                                                              bool isCalloutOpen) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitSplitButton.toggleSplitButtonCallout", dotNetObjRef, uniqueId, menuButtonId, menuButtonCalloutId, menuButtonOverlayId, isCalloutOpen);
    }
}
