namespace Bit.BlazorUI;

internal static class RatingsJsRuntimeExtensions
{
    internal static ValueTask BitRatingsSetup(this IJSRuntime jsRuntime,
                                              string id,
                                              DotNetObjectReference<BitRating> dotnetObj,
                                              string focusInHandler,
                                              string focusOutHandler)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Ratings.setup", id, dotnetObj, focusInHandler, focusOutHandler);
    }

    internal static ValueTask BitRatingsDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Ratings.dispose", id);
    }
}
