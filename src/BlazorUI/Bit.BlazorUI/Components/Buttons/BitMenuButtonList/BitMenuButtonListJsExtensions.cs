
namespace Bit.BlazorUI;

internal static class BitMenuButtonListJsExtensions
{
    internal static async Task ToggleMenuButtonListCallout<T>(this IJSRuntime jsRuntime, DotNetObjectReference<BitMenuButtonList<T>> component,
        string uniqueId, 
        string menuButtonId,
        string menuButtonCalloutId, 
        string menuButtonOverlayId,
        bool isCalloutOpen)
    {
        await jsRuntime.InvokeVoidAsync("BitMenuButtonList.toggleMenuButtonListCallout", component, uniqueId, menuButtonId, menuButtonCalloutId, menuButtonOverlayId, isCalloutOpen);
    }
}
