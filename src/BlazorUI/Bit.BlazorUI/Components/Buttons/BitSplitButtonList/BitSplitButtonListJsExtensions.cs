
namespace Bit.BlazorUI;

internal static class BitSplitButtonListJsExtensions
{
    internal static async Task ToggleSplitButtonListCallout<T>(this IJSRuntime jsRuntime,
                                                              DotNetObjectReference<BitSplitButtonList<T>> dotNetObjRef,
                                                              string uniqueId, 
                                                              string menuButtonId,
                                                              string menuButtonCalloutId, 
                                                              string menuButtonOverlayId,
                                                              bool isCalloutOpen) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitSplitButtonList.toggleSplitButtonListCallout", dotNetObjRef, uniqueId, menuButtonId, menuButtonCalloutId, menuButtonOverlayId, isCalloutOpen);
    }
}
